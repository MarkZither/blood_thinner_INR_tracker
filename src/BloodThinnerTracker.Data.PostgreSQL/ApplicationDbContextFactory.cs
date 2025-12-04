using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Data.PostgreSQL;

/// <summary>
/// Design-time factory for creating ApplicationDbContext during EF Core migrations.
/// Used by: dotnet ef migrations add/remove/update commands.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // PostgreSQL connection string for migration generation
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=bloodtracker_migration;Username=postgres;Password=postgres",
            npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

        // Create minimal design-time dependencies
        var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<ApplicationDbContext>();

        Console.WriteLine("🔧 Migration Factory: Using PostgreSQL provider");
        Console.WriteLine("   PostgreSQL: Host=localhost");

        return new ApplicationDbContext(
            optionsBuilder.Options,
            logger);
    }
}
