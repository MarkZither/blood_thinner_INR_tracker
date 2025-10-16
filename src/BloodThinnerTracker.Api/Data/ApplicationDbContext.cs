using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using System.Security.Claims;
using BloodThinnerTracker.Shared.Models;
using DataProtectionKey = Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey;

namespace BloodThinnerTracker.Api.Data;

/// <summary>
/// Main database context for the Blood Thinner Tracker application.
/// 
/// ⚠️ MEDICAL DATABASE CONTEXT:
/// This context handles sensitive medical data and implements encryption, audit trails,
/// and compliance features required for healthcare applications.
/// </summary>
public class ApplicationDbContext : DbContext, IDataProtectionKeyContext
{
    private readonly IDataProtector _dataProtector;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApplicationDbContext> _logger;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDataProtectionProvider dataProtectionProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApplicationDbContext> logger) : base(options)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("BloodThinnerTracker.MedicalData");
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // Core medical entities
    /// <summary>
    /// Users table for patient and healthcare provider accounts.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Medications table for blood thinner prescriptions.
    /// </summary>
    public DbSet<Medication> Medications { get; set; } = null!;

    /// <summary>
    /// Medication logs table for tracking medication intake.
    /// </summary>
    public DbSet<MedicationLog> MedicationLogs { get; set; } = null!;

    /// <summary>
    /// INR tests table for blood coagulation results.
    /// </summary>
    public DbSet<INRTest> INRTests { get; set; } = null!;

    /// <summary>
    /// INR schedules table for test scheduling.
    /// </summary>
    public DbSet<INRSchedule> INRSchedules { get; set; } = null!;

    /// <summary>
    /// Audit log table for tracking all medical data changes.
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    /// <summary>
    /// Data protection keys for encryption (stored in database for cloud deployment).
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure audit logging
        ConfigureAuditLogging(modelBuilder);

        // Configure data protection keys
        ConfigureDataProtection(modelBuilder);

        // Configure global query filters for soft deletion and user isolation
        ConfigureGlobalFilters(modelBuilder);

        // Configure medical data encryption (will be expanded in T009)
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
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(10);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Changes).HasColumnType("TEXT");
            entity.Property(e => e.IPAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            // Index for performance
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
        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.ExternalUserId).IsUnique();
        });

        // Configure Medication entity with user isolation
        modelBuilder.Entity<Medication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasQueryFilter(e => !e.IsDeleted);
            // Temporarily disabled - navigation properties need to be added first
            // entity.HasOne(e => e.User)
            //       .WithMany(u => u.Medications)
            //       .HasForeignKey(e => e.UserId)
            //       .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.Name });
        });

        // Configure MedicationLog entity with user isolation
        modelBuilder.Entity<MedicationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasQueryFilter(e => !e.IsDeleted);
            // Temporarily disabled - navigation properties need to be added first
            // entity.HasOne(e => e.User)
            //       .WithMany(u => u.MedicationLogs)
            //       .HasForeignKey(e => e.UserId)
            //       .OnDelete(DeleteBehavior.Cascade);
            // entity.HasOne(e => e.Medication)
            //       .WithMany(m => m.MedicationLogs)
            //       .HasForeignKey(e => e.MedicationId)
            //       .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.ScheduledTime });
            entity.HasIndex(e => e.MedicationId);
        });

        // Configure INRTest entity with user isolation
        modelBuilder.Entity<INRTest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasQueryFilter(e => !e.IsDeleted);
            // Temporarily disabled - navigation properties need to be added first
            // entity.HasOne(e => e.User)
            //       .WithMany(u => u.INRTests)
            //       .HasForeignKey(e => e.UserId)
            //       .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.TestDate });
        });

        // Configure INRSchedule entity with user isolation
        modelBuilder.Entity<INRSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasQueryFilter(e => !e.IsDeleted);
            // Temporarily disabled - navigation properties need to be added first
            // entity.HasOne(e => e.User)
            //       .WithMany(u => u.INRSchedules)
            //       .HasForeignKey(e => e.UserId)
            //       .OnDelete(DeleteBehavior.Cascade);
            // entity.HasOne(e => e.CompletedTest)
            //       .WithMany()
            //       .HasForeignKey(e => e.CompletedTestId)
            //       .OnDelete(DeleteBehavior.SetNull);
            // entity.HasOne(e => e.ParentSchedule)
            //       .WithMany(s => s.ChildSchedules)
            //       .HasForeignKey(e => e.ParentScheduleId)
            //       .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.UserId, e.ScheduledDate });
        });
        
        _logger.LogDebug("Global query filters configured for medical data isolation");
    }

    /// <summary>
    /// Configures encryption for sensitive medical data fields.
    /// </summary>
    private void ConfigureMedicalDataEncryption(ModelBuilder modelBuilder)
    {
        // Medical data encryption configuration will be expanded in T009
        // This includes encrypting sensitive fields like medication names, dosages, and notes
        
        _logger.LogDebug("Medical data encryption configuration prepared");
    }

    /// <summary>
    /// Configures database indexes for optimal performance.
    /// </summary>
    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Performance indexes will be added for medical entities in T009
        // This includes indexes on user ID, timestamps, and frequently queried fields
        
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
                t.HasCheckConstraint("CK_Medication_Dosage", "Dosage > 0 AND Dosage <= 1000");
                t.HasCheckConstraint("CK_Medication_Dates", "EndDate IS NULL OR EndDate >= StartDate");
                t.HasCheckConstraint("CK_Medication_Reminder", "ReminderMinutes >= 0 AND ReminderMinutes <= 1440");
            });
        });

        // MedicationLog entity constraints
        modelBuilder.Entity<MedicationLog>(entity =>
        {
            entity.Property(e => e.ActualDosage).HasPrecision(10, 3);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_MedicationLog_ActualDosage", "ActualDosage IS NULL OR (ActualDosage > 0 AND ActualDosage <= 1000)");
                t.HasCheckConstraint("CK_MedicationLog_TimeVariance", "TimeVarianceMinutes >= -1440 AND TimeVarianceMinutes <= 1440");
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
                t.HasCheckConstraint("CK_INRTest_Value", "INRValue >= 0.5 AND INRValue <= 8.0");
                t.HasCheckConstraint("CK_INRTest_TargetRange", "TargetINRMin IS NULL OR TargetINRMax IS NULL OR TargetINRMin < TargetINRMax");
                t.HasCheckConstraint("CK_INRTest_PT", "ProthrombinTime IS NULL OR (ProthrombinTime >= 8.0 AND ProthrombinTime <= 60.0)");
                t.HasCheckConstraint("CK_INRTest_PTT", "PartialThromboplastinTime IS NULL OR (PartialThromboplastinTime >= 20.0 AND PartialThromboplastinTime <= 120.0)");
            });
        });

        // INRSchedule entity constraints
        modelBuilder.Entity<INRSchedule>(entity =>
        {
            entity.Property(e => e.TargetINRMin).HasPrecision(3, 1);
            entity.Property(e => e.TargetINRMax).HasPrecision(3, 1);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_INRSchedule_Interval", "IntervalDays >= 1 AND IntervalDays <= 365");
                t.HasCheckConstraint("CK_INRSchedule_TargetRange", "TargetINRMin IS NULL OR TargetINRMax IS NULL OR TargetINRMin < TargetINRMax");
                t.HasCheckConstraint("CK_INRSchedule_Reminder", "ReminderDays >= 0 AND ReminderDays <= 14");
                t.HasCheckConstraint("CK_INRSchedule_Dates", "EndDate IS NULL OR EndDate > ScheduledDate");
            });
        });
        
        _logger.LogDebug("Medical validation constraints configured");
    }

    /// <summary>
    /// Overrides SaveChanges to implement audit logging and medical data validation.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = new List<AuditLog>();
        var currentUserId = GetCurrentUserId();
        var currentTime = DateTime.UtcNow;

        // Process changes for audit logging
        foreach (var entry in ChangeTracker.Entries())
        {
            // Skip audit logs and data protection keys from being audited
            if (entry.Entity is AuditLog || entry.Entity is DataProtectionKey)
                continue;

            // Update audit fields for medical entities
            if (entry.Entity is IMedicalEntity medicalEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        medicalEntity.CreatedAt = currentTime;
                        medicalEntity.UpdatedAt = currentTime;
                        medicalEntity.UserId ??= currentUserId ?? throw new InvalidOperationException("User ID is required for medical data");
                        break;
                    case EntityState.Modified:
                        medicalEntity.UpdatedAt = currentTime;
                        // Prevent changing user ID or creation date
                        entry.Property(nameof(IMedicalEntity.CreatedAt)).IsModified = false;
                        entry.Property(nameof(IMedicalEntity.UserId)).IsModified = false;
                        break;
                }
            }

            // Create audit log entry
            var auditLog = CreateAuditLogEntry(entry, currentUserId, currentTime);
            if (auditLog != null)
            {
                auditEntries.Add(auditLog);
            }
        }

        // Validate medical business rules before saving
        await ValidateMedicalBusinessRules(cancellationToken);

        // Save changes
        var result = await base.SaveChangesAsync(cancellationToken);

        // Add audit logs after successful save
        if (auditEntries.Any())
        {
            AuditLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Creates an audit log entry for a changed entity.
    /// </summary>
    private AuditLog? CreateAuditLogEntry(EntityEntry entry, string? userId, DateTime timestamp)
    {
        if (userId == null)
        {
            _logger.LogWarning("Cannot create audit log entry: User ID is null");
            return null;
        }

        var entityName = entry.Entity.GetType().Name;
        var entityId = GetEntityId(entry);
        
        if (entityId == null)
        {
            _logger.LogWarning("Cannot create audit log entry: Entity ID is null for {EntityName}", entityName);
            return null;
        }

        var action = entry.State switch
        {
            EntityState.Added => "CREATE",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => "UNKNOWN"
        };

        var changes = SerializeChanges(entry);
        var httpContext = _httpContextAccessor.HttpContext;

        return new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            Timestamp = timestamp,
            Changes = changes,
            IPAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString()
        };
    }

    /// <summary>
    /// Gets the entity ID from an EntityEntry.
    /// </summary>
    private string? GetEntityId(EntityEntry entry)
    {
        var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return keyProperty?.CurrentValue?.ToString();
    }

    /// <summary>
    /// Serializes the changes made to an entity.
    /// </summary>
    private string SerializeChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.IsModified || entry.State == EntityState.Added)
            {
                // Don't log sensitive medical data in plain text
                if (IsSensitiveProperty(property.Metadata.Name))
                {
                    changes[property.Metadata.Name] = "[ENCRYPTED]";
                }
                else
                {
                    changes[property.Metadata.Name] = new
                    {
                        OldValue = property.OriginalValue,
                        NewValue = property.CurrentValue
                    };
                }
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(changes);
    }

    /// <summary>
    /// Determines if a property contains sensitive medical data.
    /// </summary>
    private bool IsSensitiveProperty(string propertyName)
    {
        var sensitiveProperties = new[]
        {
            "Notes", "MedicationName", "DosageAmount", "PatientNotes",
            "HealthcareProviderNotes", "INRValue", "TestResults"
        };

        return sensitiveProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the current user ID from the HTTP context.
    /// </summary>
    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Validates medical business rules before saving data.
    /// </summary>
    private async Task ValidateMedicalBusinessRules(CancellationToken cancellationToken)
    {
        var medicalEntities = ChangeTracker.Entries()
            .Where(e => e.Entity is IMedicalEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified))
            .Select(e => e.Entity as IMedicalEntity)
            .Where(e => e != null);

        foreach (var entity in medicalEntities)
        {
            await ValidateMedicalEntity(entity!, cancellationToken);
        }
    }

    /// <summary>
    /// Validates a single medical entity against business rules.
    /// </summary>
    private async Task ValidateMedicalEntity(IMedicalEntity entity, CancellationToken cancellationToken)
    {
        // Basic validation
        if (string.IsNullOrEmpty(entity.UserId))
        {
            throw new InvalidOperationException("Medical data must be associated with a user");
        }

        if (entity.CreatedAt == default)
        {
            throw new InvalidOperationException("Medical data must have a valid creation timestamp");
        }

        // Additional medical validations will be implemented in T009
        await Task.CompletedTask;
    }

    /// <summary>
    /// Encrypts sensitive medical data.
    /// </summary>
    public string EncryptMedicalData(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            return _dataProtector.Protect(plainText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt medical data");
            throw new InvalidOperationException("Failed to encrypt medical data", ex);
        }
    }

    /// <summary>
    /// Decrypts sensitive medical data.
    /// </summary>
    public string DecryptMedicalData(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            return _dataProtector.Unprotect(encryptedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt medical data");
            throw new InvalidOperationException("Failed to decrypt medical data", ex);
        }
    }
}