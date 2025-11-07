using BloodThinnerTracker.Data.Shared;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Data.SQLite;

/// <summary>
/// SQLite-specific implementation of ApplicationDbContext.
/// Inherits all configuration from ApplicationDbContextBase.
/// </summary>
public class ApplicationDbContext : ApplicationDbContextBase
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDataProtectionProvider dataProtectionProvider,
        ICurrentUserService currentUserService,
        ILogger<ApplicationDbContext> logger)
        : base(options, dataProtectionProvider, currentUserService, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite doesn't support JSONB - override to use TEXT for JSON columns
        modelBuilder.Entity<BloodThinnerTracker.Shared.Models.MedicationDosagePattern>(entity =>
        {
            entity.Property(p => p.PatternSequence)
                .HasColumnType("TEXT"); // SQLite uses TEXT for JSON storage
        });
    }
}
