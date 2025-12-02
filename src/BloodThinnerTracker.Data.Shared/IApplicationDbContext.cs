using BloodThinnerTracker.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodThinnerTracker.Data.Shared;

/// <summary>
/// Interface for the application database context.
/// Used for dependency injection and testability across all database providers.
/// </summary>
public interface IApplicationDbContext
{
    // Core medical entities
    DbSet<User> Users { get; set; }
    DbSet<Medication> Medications { get; set; }
    DbSet<MedicationLog> MedicationLogs { get; set; }
    DbSet<MedicationDosagePattern> MedicationDosagePatterns { get; set; }
    DbSet<INRTest> INRTests { get; set; }
    DbSet<INRSchedule> INRSchedules { get; set; }
    DbSet<AuditLog> AuditLogs { get; set; }
    DbSet<RefreshToken> RefreshTokens { get; set; }

    // Database operations
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
}
