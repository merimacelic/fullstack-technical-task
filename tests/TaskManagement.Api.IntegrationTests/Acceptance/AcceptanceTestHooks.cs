using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Respawn;
using TaskManagement.Api.IntegrationTests.Infrastructure;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Api.IntegrationTests.Acceptance;

// Shared lifecycle for every scenario in the acceptance suite.
// BeforeTestRun / AfterTestRun fire once per `dotnet test` run (regardless of
// how many feature files are present), so a single Testcontainers SQL Server
// and a single WebApplicationFactory serve the whole Reqnroll run. Respawn
// resets the database between scenarios so tests stay independent.
[Binding]
public static class AcceptanceTestHooks
{
    private static TaskManagementApiFactory? _factory;
    private static Respawner? _respawner;

    public static TaskManagementApiFactory Factory =>
        _factory ?? throw new InvalidOperationException(
            "Acceptance factory accessed before BeforeTestRun completed.");

    [BeforeTestRun]
    public static async Task InitializeAsync()
    {
        _factory = new TaskManagementApiFactory();
        await ((IAsyncLifetime)_factory).InitializeAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            SchemasToInclude = ["dbo"],
            TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory")],
            DbAdapter = DbAdapter.SqlServer,
        });
    }

    [BeforeScenario]
    public static async Task ResetDatabaseAsync()
    {
        if (_factory is null || _respawner is null)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    [AfterTestRun]
    public static async Task DisposeAsync()
    {
        if (_factory is null)
        {
            return;
        }

        await ((IAsyncLifetime)_factory).DisposeAsync();
        await _factory.DisposeAsync();
        _factory = null;
    }
}
