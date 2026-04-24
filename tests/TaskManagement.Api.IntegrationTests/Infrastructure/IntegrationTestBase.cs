using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using TaskManagement.Application.Users.Responses;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Api.IntegrationTests.Infrastructure;

[Collection(TaskManagementCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly TaskManagementApiFactory _factory;
    private Respawner? _respawner;

    protected IntegrationTestBase(TaskManagementApiFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();
    }

    protected HttpClient Client { get; }

    protected IServiceScope CreateScope() => _factory.Services.CreateScope();

    protected HttpClient CreateUnauthenticatedClient() => _factory.CreateClient();

    protected async Task<T> UseDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(db);
    }

    protected async Task UseDbAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(db);
    }

    // Registers a user and returns the auth response.
    // Email is random so concurrent tests in the same fixture don't collide.
    protected async Task<AuthResponse> RegisterAsync(HttpClient? client = null, string? email = null)
    {
        client ??= Client;
        email ??= $"user-{Guid.NewGuid():N}@icon.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Passw0rd!",
        });
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync<AuthResponse>(response);
    }

    // Registers a user and attaches the bearer token to the given client for all subsequent calls.
    protected async Task<AuthResponse> AuthenticateAsync(HttpClient? client = null, string? email = null)
    {
        client ??= Client;
        var auth = await RegisterAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }

    public async Task InitializeAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            SchemasToInclude = ["dbo"],
            TablesToIgnore = [new("__EFMigrationsHistory")],
            DbAdapter = DbAdapter.SqlServer,
        });

        await _respawner.ResetAsync(conn);
        Client.DefaultRequestHeaders.Authorization = null;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>();
        return value ?? throw new InvalidOperationException("Response body was null.");
    }
}
