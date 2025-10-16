namespace BloodThinnerTracker.Shared.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Interface for medical entities that require audit trails and user isolation.
///
/// ⚠️ MEDICAL ENTITY INTERFACE:
/// All medical data entities must implement this interface to ensure proper
/// audit logging, user data isolation, and compliance with healthcare regulations.
/// </summary>
public interface IMedicalEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Gets or sets when this record was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when this record was last updated.
    /// </summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who owns this record.
    /// Medical data must be isolated per user for privacy and security.
    /// </summary>
    string UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this record is soft deleted for medical data retention compliance.
    /// Medical data cannot be permanently deleted immediately due to legal requirements.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets when this record was soft deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Base class for all medical entities with common audit and compliance features.
///
/// ⚠️ MEDICAL BASE ENTITY:
/// This base class implements common medical data requirements including
/// audit trails, soft deletion, and user data isolation.
/// </summary>
public abstract class MedicalEntityBase : IMedicalEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets when this record was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this record was last updated.
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who owns this record.
    /// Medical data must be isolated per user for privacy and security.
    /// </summary>
    [Required]
    [StringLength(450)] // Standard ASP.NET Identity user ID length
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this record is soft deleted for medical data retention compliance.
    /// Medical data cannot be permanently deleted immediately due to legal requirements.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets when this record was soft deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Audit log entity for tracking all changes to medical data.
///
/// ⚠️ MEDICAL AUDIT LOG:
/// This entity stores comprehensive audit information for all medical data changes
/// to ensure compliance with healthcare regulations and security requirements.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit log entry.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the entity that was changed.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the entity that was changed.
    /// </summary>
    [Required]
    [StringLength(36)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action that was performed (CREATE, UPDATE, DELETE).
    /// </summary>
    [Required]
    [StringLength(10)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user who performed the action.
    /// </summary>
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the action was performed.
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the serialized changes made to the entity.
    /// Sensitive medical data is encrypted or masked in this field.
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the user who performed the action.
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? IPAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent of the client that performed the action.
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }
}