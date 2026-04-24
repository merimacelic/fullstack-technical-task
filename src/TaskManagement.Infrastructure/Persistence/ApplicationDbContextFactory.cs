using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManagement.Infrastructure.Persistence;

// Design-time factory used by `dotnet ef migrations` so it can create the DbContext
// without bootstrapping the full host (which would require auth, configuration, etc.).
internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("EF_DESIGN_CONNECTION")
            ?? "Server=localhost,1433;Database=TaskManagement;User Id=sa;Password=Your_Password123;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
