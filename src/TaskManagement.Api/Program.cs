using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;
using TaskManagement.Api.Auth;
using TaskManagement.Api.Endpoints;
using TaskManagement.Api.Infrastructure;
using TaskManagement.Api.Localization;
using TaskManagement.Application;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Auth;
using TaskManagement.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddEndpoints();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    // Localisation. Marker types and resx files share a `…Resources`
    // namespace, so ResourcesPath is left unset — adding it would double-
    // prefix the lookup and every resource would miss.
    // RequestLocalizationMiddleware picks the culture from, in order:
    // ?culture= query string, a "culture" cookie, then Accept-Language.
    builder.Services.AddLocalization();
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        var supported = new[] { new CultureInfo("en"), new CultureInfo("mt") };
        options.DefaultRequestCulture = new RequestCulture("en");
        options.SupportedCultures = supported;
        options.SupportedUICultures = supported;
    });
    builder.Services.AddScoped<IErrorLocalizer, ErrorLocalizer>();

    // JWT bearer authentication — token validation parameters read from the same
    // JwtOptions used by the token issuer, so a single config change stays in sync.
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                ?? throw new InvalidOperationException("Jwt configuration section is missing.");
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddAuthorization();

    // CORS — named policy for the (future) React SPA; origins read from config.
    const string SpaCorsPolicy = "SpaCors";
    var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy(SpaCorsPolicy, policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    }));

    // Rate limiting — partitioned global limiter per IP + stricter bucket for auth
    // endpoints. Defaults off in the "Testing" environment (set by the integration-
    // test factory) so the bulk of the suite isn't throttled. An explicit config
    // value wins either way so a dedicated 429 test can opt back in without
    // spinning up a non-Testing host.
    var rateLimitingEnabled = builder.Configuration.GetValue<bool?>("RateLimiting:Enabled")
        ?? !builder.Environment.IsEnvironment("Testing");
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        if (rateLimitingEnabled)
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                RateLimitPartition.GetFixedWindowLimiter(
                    http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy("auth", http =>
                RateLimitPartition.GetTokenBucketLimiter(
                    http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 10,
                        TokensPerPeriod = 5,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
        }
        else
        {
            // Policy must still exist so endpoints referencing it resolve; a NoLimiter
            // partition means requests pass through unthrottled.
            options.AddPolicy("auth", _ => RateLimitPartition.GetNoLimiter<string>("disabled"));
        }
    });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Liveness = self only (is the process up?). Readiness = self + DB (is it
    // actually serving?). Splitting them lets Kubernetes remove a pod from the
    // load balancer on a DB hiccup without killing and restarting it.
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "Task Management API",
            Version = "v1",
            Description = "RESTful API for managing tasks — ICON Studios trial.",
        });
        // Pull XML docs from every assembly that ships DTOs or endpoints so Swagger's
        // schema view carries property-level descriptions (CreateTaskCommand, TaskResponse,
        // PagedResult, etc.), not just the per-endpoint summaries.
        foreach (var xmlFile in Directory.EnumerateFiles(AppContext.BaseDirectory, "TaskManagement.*.xml"))
        {
            options.IncludeXmlComments(xmlFile);
        }

        // Bearer scheme so the Swagger "Authorize" button attaches the JWT to every call.
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the access token returned by /api/auth/login.",
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        });
    });

    var app = builder.Build();

    // CorrelationId — accept the inbound header or mint a new one, echo it back,
    // and push it onto the Serilog LogContext so every subsequent log line in the
    // request scope carries it. Runs before request logging so the "started request"
    // line is correlated too.
    app.Use(async (context, next) =>
    {
        const string HeaderName = "X-Correlation-Id";
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var inbound) && !string.IsNullOrWhiteSpace(inbound)
            ? inbound.ToString()
            : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next();
        }
    });

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    // Culture negotiation — placed before endpoints so every downstream
    // component (validators, IErrorLocalizer, IStringLocalizer<ApiResource>)
    // observes the resolved CultureInfo on its request-scoped thread.
    app.UseRequestLocalization();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    // Default API security headers (frame-deny, no-sniff, referrer-policy, permissions-policy locked).
    app.UseSecurityHeaders();

    app.UseCors(SpaCorsPolicy);
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    // Swagger / Scalar are exposed in every environment. The endpoints behind
    // them all require JWT, so the docs themselves don't widen the attack
    // surface; making them available on the live deployment lets a reviewer
    // poke the API directly without spinning up a local instance.
    app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Task Management API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Task Management API — Swagger";
    });
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Task Management API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithOpenApiRoutePattern("/openapi/v1.json");
    });

    app.MapEndpoints();

    // Health endpoints are deliberately excluded from OpenAPI — they are operational
    // probes for orchestrators (Kubernetes, load balancers), not part of the public API.
    // /health  → liveness: runs only the self check (no DB), so orchestrators don't
    //            restart the pod on a transient DB failure.
    // /health/ready → readiness: includes the DB check, signals "this pod is serving".
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = check => !check.Tags.Contains("ready"),
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
    });

    await MigrateDatabaseIfConfiguredAsync(app);
    await SeedDemoDataIfConfiguredAsync(app);

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static async Task MigrateDatabaseIfConfiguredAsync(WebApplication app)
{
    var runMigrations = app.Configuration.GetValue("Database:RunMigrationsOnStartup", defaultValue: false);
    if (!runMigrations)
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Absorb brief SQL-Server-not-quite-ready windows when running under docker-compose.
    // Narrowed to SQL connectivity/timeout failures — config or auth errors won't heal by retrying.
    var retry = Policy
        .Handle<SqlException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 6,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (ex, delay, attempt, _) =>
                logger.LogWarning(ex, "Database migration attempt {Attempt} failed; retrying in {Delay}.", attempt, delay));

    await retry.ExecuteAsync(() => db.Database.MigrateAsync());
}

static async Task SeedDemoDataIfConfiguredAsync(WebApplication app)
{
    // Gated behind Seeding:DemoData so empty installs stay empty by default.
    // Intended to be flipped on for the hosted demo + optionally local compose.
    var seedDemo = app.Configuration.GetValue("Seeding:DemoData", defaultValue: false);
    if (!seedDemo)
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    await seeder.SeedAsync();
}

// Enables WebApplicationFactory<Program> for integration tests.
public partial class Program;
