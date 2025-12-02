using BloodThinnerTracker.Data.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Data.SqlServer;

/// <summary>
/// SQL Server-specific implementation of ApplicationDbContext.
/// Inherits all configuration from ApplicationDbContextBase.
/// </summary>
public class ApplicationDbContext : ApplicationDbContextBase
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ILogger<ApplicationDbContext> logger)
        : base(options, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQL Server doesn't support JSONB - override to use NVARCHAR(MAX) for JSON columns
        modelBuilder.Entity<BloodThinnerTracker.Shared.Models.MedicationDosagePattern>(entity =>
        {
            entity.Property(p => p.PatternSequence)
                .HasColumnType("nvarchar(max)"); // SQL Server uses NVARCHAR(MAX) for JSON storage
        });

        // SQL Server doesn't allow multiple cascade paths to the same table
        // This prevents "may cause cycles or multiple cascade paths" errors
        // We disable cascade delete on all foreign keys and handle deletions in application code
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
