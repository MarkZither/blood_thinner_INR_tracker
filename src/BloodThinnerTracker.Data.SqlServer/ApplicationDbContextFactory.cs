using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Data.SqlServer;

/// <summary>
/// Design-time factory for creating ApplicationDbContext during EF Core migrations.
/// Used by: dotnet ef migrations add/remove/update commands.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // SQL Server connection string for migration generation
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=bloodtracker_migration;Trusted_Connection=true;MultipleActiveResultSets=true",
            sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });

        // Create minimal design-time dependencies
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var currentUserService = new DesignTimeCurrentUserService();
        var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<ApplicationDbContext>();

        Console.WriteLine("🔧 Migration Factory: Using SQL Server provider");
        Console.WriteLine("   SQL Server: (localdb)\\mssqllocaldb");

        return new ApplicationDbContext(
            optionsBuilder.Options,
            dataProtectionProvider,
            currentUserService,
            logger);
    }
}
