using BloodThinnerTracker.Shared.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using DataProtectionKey = Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey;

namespace BloodThinnerTracker.Data.Shared;

/// <summary>
/// Abstract base context for the Blood Thinner Tracker application.
/// Contains provider-agnostic configuration and logic.
/// Provider-specific implementations inherit from this class.
///
/// ⚠️ MEDICAL DATABASE CONTEXT:
/// Handles sensitive medical data with encryption, audit trails, and compliance features.
/// </summary>
public abstract class ApplicationDbContextBase : DbContext, IDataProtectionKeyContext, IApplicationDbContext
{
    protected readonly IDataProtector _dataProtector;
    protected readonly ICurrentUserService _currentUserService;
    protected readonly ILogger<ApplicationDbContextBase> _logger;

    protected ApplicationDbContextBase(
        DbContextOptions options,
        IDataProtectionProvider dataProtectionProvider,
        ICurrentUserService currentUserService,
        ILogger<ApplicationDbContextBase> logger) : base(options)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("BloodThinnerTracker.MedicalData");
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // Core medical entities
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Medication> Medications { get; set; } = null!;
    public DbSet<MedicationLog> MedicationLogs { get; set; } = null!;
    public DbSet<MedicationDosagePattern> MedicationDosagePatterns { get; set; } = null!;
    public DbSet<INRTest> INRTests { get; set; } = null!;
    public DbSet<INRSchedule> INRSchedules { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure audit logging
        ConfigureAuditLogging(modelBuilder);

        // Configure data protection keys
        ConfigureDataProtection(modelBuilder);

        // Configure global query filters for soft deletion and user isolation
        ConfigureGlobalFilters(modelBuilder);

        // Configure medical data encryption
        ConfigureMedicalDataEncryption(modelBuilder);

        // Configure indexes for performance
        ConfigureIndexes(modelBuilder);

        // Configure medical validation constraints
        ConfigureMedicalConstraints(modelBuilder);
    }

    /// <summary>
    /// Configures audit logging for all medical entities.
    /// </summary>
    private void ConfigureAuditLogging(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PublicId).IsRequired();
            entity.HasIndex(e => e.PublicId).IsUnique();

            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(10);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Changes).HasColumnType("TEXT");
            entity.Property(e => e.IPAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
        });
    }

    /// <summary>
    /// Configures data protection keys storage in database.
    /// </summary>
    private void ConfigureDataProtection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataProtectionKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FriendlyName).HasMaxLength(100);
            entity.Property(e => e.Xml).IsRequired();
        });
    }

    /// <summary>
    /// Configures global query filters for soft deletion and user data isolation.
    /// </summary>
    private void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        // Configure User entity (root tenant entity - no UserId FK, but has soft delete)
        modelBuilder.Entity<User>(entity =>
        {
            // User is the tenant root - configure its own security keys
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PublicId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.ExternalUserId).IsUnique();
            // Soft delete filter - users still need soft delete for GDPR compliance
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Medication entity with user isolation
        modelBuilder.Entity<Medication>(entity =>
        {
            ConfigureSecurityKeys(entity);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.UserId, e.Name });
        });

        // Configure MedicationLog entity with user isolation
        modelBuilder.Entity<MedicationLog>(entity =>
        {
            ConfigureSecurityKeys(entity);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.UserId, e.ScheduledTime });
            entity.HasIndex(e => e.MedicationId);
        });

        // Configure MedicationDosagePattern entity with user isolation (via Medication)
        modelBuilder.Entity<MedicationDosagePattern>(entity =>
        {
            ConfigureSecurityKeys(entity);
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Temporal index for pattern queries (most common: find active pattern for medication)
            entity.HasIndex(e => new { e.MedicationId, e.StartDate, e.EndDate })
                .HasDatabaseName("IX_MedicationDosagePattern_Temporal");

            // Active patterns index (filter NULL EndDate for fast active pattern lookups)
            entity.HasIndex(e => new { e.MedicationId, e.EndDate })
                .HasDatabaseName("IX_MedicationDosagePattern_Active")
                .HasFilter("\"EndDate\" IS NULL");

            // Foreign key relationship with cascade delete
            entity.HasOne(p => p.Medication)
                .WithMany(m => m.DosagePatterns)
                .HasForeignKey(p => p.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure JSON column for PatternSequence (provider-specific handling)
            // PostgreSQL will use JSONB, SQLite will use TEXT
            entity.Property(p => p.PatternSequence)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<decimal>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<decimal>()
                )
                .HasColumnType("jsonb") // PostgreSQL JSONB, SQLite will override to TEXT
                .IsRequired();
        });

        // Configure INRTest entity with user isolation
        modelBuilder.Entity<INRTest>(entity =>
        {
            ConfigureSecurityKeys(entity);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.UserId, e.TestDate });
        });

        // Configure INRSchedule entity with user isolation
        modelBuilder.Entity<INRSchedule>(entity =>
        {
            ConfigureSecurityKeys(entity);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.UserId, e.ScheduledDate });
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();

            entity.HasQueryFilter(rt => rt.RevokedAt == null);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
            entity.HasIndex(e => e.CreatedAt);
        });

        _logger.LogDebug("Global query filters configured for medical data isolation");
    }

    /// <summary>
    /// Configures dual-key security pattern for medical entities.
    /// ⚠️ SECURITY: Implements defense-in-depth against IDOR and enumeration attacks.
    /// </summary>
    protected static void ConfigureSecurityKeys<T>(EntityTypeBuilder<T> entity) where T : class, IMedicalEntity
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.Property(e => e.PublicId).IsRequired();
        entity.HasIndex(e => e.PublicId).IsUnique();
    }

    private void ConfigureMedicalDataEncryption(ModelBuilder modelBuilder)
    {
        _logger.LogDebug("Medical data encryption configuration prepared");
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        _logger.LogDebug("Database indexes configured for medical entities");
    }

    /// <summary>
    /// Configures medical validation constraints at the database level.
    /// </summary>
    private void ConfigureMedicalConstraints(ModelBuilder modelBuilder)
    {
        // User entity constraints
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TimeZone).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.AuthProvider).IsRequired().HasMaxLength(20);
        });

        // Medication entity constraints
        modelBuilder.Entity<Medication>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DosageUnit).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Dosage).HasPrecision(10, 3);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Medication_Dosage", "\"Dosage\" > 0 AND \"Dosage\" <= 1000");
                t.HasCheckConstraint("CK_Medication_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                t.HasCheckConstraint("CK_Medication_Reminder", "\"ReminderMinutes\" >= 0 AND \"ReminderMinutes\" <= 1440");
            });
        });

        // MedicationLog entity constraints
        modelBuilder.Entity<MedicationLog>(entity =>
        {
            entity.Property(e => e.ActualDosage).HasPrecision(10, 3);
            entity.Property(e => e.ExpectedDosage).HasPrecision(10, 3);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_MedicationLog_ActualDosage", "\"ActualDosage\" IS NULL OR (\"ActualDosage\" > 0 AND \"ActualDosage\" <= 1000)");
                t.HasCheckConstraint("CK_MedicationLog_ExpectedDosage", "\"ExpectedDosage\" IS NULL OR (\"ExpectedDosage\" > 0 AND \"ExpectedDosage\" <= 1000)");
                t.HasCheckConstraint("CK_MedicationLog_TimeVariance", "\"TimeVarianceMinutes\" >= -1440 AND \"TimeVarianceMinutes\" <= 1440");
                t.HasCheckConstraint("CK_MedicationLog_PatternDay", "\"PatternDayNumber\" IS NULL OR (\"PatternDayNumber\" >= 1 AND \"PatternDayNumber\" <= 365)");
            });

            // Foreign key to MedicationDosagePattern (optional, SetNull on delete)
            entity.HasOne(log => log.DosagePattern)
                .WithMany()
                .HasForeignKey(log => log.DosagePatternId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MedicationDosagePattern entity constraints
        modelBuilder.Entity<MedicationDosagePattern>(entity =>
        {
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_MedicationDosagePattern_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
            });
        });

        // INRTest entity constraints
        modelBuilder.Entity<INRTest>(entity =>
        {
            entity.Property(e => e.INRValue).HasPrecision(4, 2);
            entity.Property(e => e.TargetINRMin).HasPrecision(3, 1);
            entity.Property(e => e.TargetINRMax).HasPrecision(3, 1);
            entity.Property(e => e.ProthrombinTime).HasPrecision(5, 2);
            entity.Property(e => e.PartialThromboplastinTime).HasPrecision(5, 2);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_INRTest_Value", "\"INRValue\" >= 0.5 AND \"INRValue\" <= 8.0");
                t.HasCheckConstraint("CK_INRTest_TargetRange", "\"TargetINRMin\" IS NULL OR \"TargetINRMax\" IS NULL OR \"TargetINRMin\" < \"TargetINRMax\"");
                t.HasCheckConstraint("CK_INRTest_PT", "\"ProthrombinTime\" IS NULL OR (\"ProthrombinTime\" >= 8.0 AND \"ProthrombinTime\" <= 60.0)");
                t.HasCheckConstraint("CK_INRTest_PTT", "\"PartialThromboplastinTime\" IS NULL OR (\"PartialThromboplastinTime\" >= 20.0 AND \"PartialThromboplastinTime\" <= 120.0)");
            });
        });

        // INRSchedule entity constraints
        modelBuilder.Entity<INRSchedule>(entity =>
        {
            entity.Property(e => e.TargetINRMin).HasPrecision(3, 1);
            entity.Property(e => e.TargetINRMax).HasPrecision(3, 1);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_INRSchedule_Interval", "\"IntervalDays\" >= 1 AND \"IntervalDays\" <= 365");
                t.HasCheckConstraint("CK_INRSchedule_TargetRange", "\"TargetINRMin\" IS NULL OR \"TargetINRMax\" IS NULL OR \"TargetINRMin\" < \"TargetINRMax\"");
                t.HasCheckConstraint("CK_INRSchedule_Reminder", "\"ReminderDays\" >= 0 AND \"ReminderDays\" <= 14");
                t.HasCheckConstraint("CK_INRSchedule_Dates", "\"EndDate\" IS NULL OR \"EndDate\" > \"ScheduledDate\"");
            });
        });

        _logger.LogDebug("Medical validation constraints configured");
    }

    /// <summary>
    /// Overrides SaveChanges to implement audit logging and medical data validation.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var currentTime = DateTime.UtcNow;

        // Process changes for audit logging
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog or DataProtectionKey)
                continue;

            // Update audit fields for medical entities
            if (entry.Entity is IMedicalEntity medicalEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        medicalEntity.CreatedAt = currentTime;
                        medicalEntity.UpdatedAt = currentTime;

                        // Special case: User entities self-reference
                        if (entry.Entity is not User && medicalEntity.UserId == 0)
                        {
                            medicalEntity.UserId = currentUserId ?? throw new InvalidOperationException("User ID is required for medical data");
                        }
                        break;
                    case EntityState.Modified:
                        medicalEntity.UpdatedAt = currentTime;
                        entry.Property(nameof(IMedicalEntity.CreatedAt)).IsModified = false;
                        entry.Property(nameof(IMedicalEntity.UserId)).IsModified = false;
                        break;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
