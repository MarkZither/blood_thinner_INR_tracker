using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Data.SQLite;

/// <summary>
/// Design-time factory for creating ApplicationDbContext during EF Core migrations.
/// Used by: dotnet ef migrations add/remove/update commands.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // SQLite connection string for migration generation
        optionsBuilder.UseSqlite("Data Source=bloodtracker_migration.db");

        // Create minimal design-time dependencies
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var currentUserService = new DesignTimeCurrentUserService();
        var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<ApplicationDbContext>();

        Console.WriteLine("🔧 Migration Factory: Using SQLite provider");
        Console.WriteLine("   SQLite: Data Source=bloodtracker_migration.db");

        return new ApplicationDbContext(
            optionsBuilder.Options,
            dataProtectionProvider,
            currentUserService,
            logger);
    }
}
