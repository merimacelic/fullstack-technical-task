using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Api.IntegrationTests.Infrastructure;

public sealed class TaskManagementApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestJwtSecret = "integration-tests-only-secret-key-do-not-reuse-32ch";
    public const string TestJwtIssuer = "TaskManagement.Api";
    public const string TestJwtAudience = "TaskManagement.Client";

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.DatabaseConnectionStringName}"] = ConnectionString,
                ["Jwt:SecretKey"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["Database:RunMigrationsOnStartup"] = "false",
                // Rate limiting is off by default so the bulk of the suite isn't
                // accidentally throttled; a dedicated test enables it explicitly
                // to exercise the 429 path.
                ["RateLimiting:Enabled"] = "false",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Swap the DbContext registration to use Testcontainers MSSQL for the duration of the test run.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseSqlServer(ConnectionString, sql =>
                    sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }
}
