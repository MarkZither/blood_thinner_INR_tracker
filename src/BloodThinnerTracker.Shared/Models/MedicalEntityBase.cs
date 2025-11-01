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
    /// Gets or sets the internal database identifier for this entity.
    /// ⚠️ SECURITY: Internal use only - never expose in APIs.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Gets or sets the public-facing identifier for API consumers.
    /// ⚠️ SECURITY: Non-sequential GUID prevents IDOR and enumeration attacks.
    /// </summary>
    Guid PublicId { get; set; }

    /// <summary>
    /// Gets or sets when this record was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when this record was last updated.
    /// </summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who owns this record (internal foreign key).
    /// ⚠️ SECURITY: This is the internal int FK for database efficiency.
    /// API consumers should never see this value - use PublicId instead.
    /// Medical data must be isolated per user for privacy and security.
    /// </summary>
    int UserId { get; set; }

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
///
/// SECURITY: Uses dual-key pattern for defense-in-depth:
/// - Internal PK: Int with auto-increment for efficient database indexing and relationships
/// - Public Key: GUID exposed to API consumers to prevent IDOR and enumeration attacks
/// </summary>
public abstract class MedicalEntityBase : IMedicalEntity
{
    /// <summary>
    /// Gets or sets the internal database identifier for this entity.
    /// ⚠️ SECURITY: Internal use only - NEVER expose this in APIs!
    /// Use PublicId for all external references.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the public-facing identifier for API consumers.
    /// ⚠️ SECURITY: Always use this in API responses instead of Id.
    /// Non-sequential GUID prevents IDOR attacks and enumeration attacks.
    /// </summary>
    [Required]
    public Guid PublicId { get; set; } = Guid.NewGuid();

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
    /// Gets or sets the user ID who owns this record (internal foreign key).
    /// ⚠️ SECURITY: This is the internal int FK for database efficiency.
    /// API consumers should never see this value - use PublicId instead.
    /// Medical data must be isolated per user for privacy and security.
    /// </summary>
    [Required]
    public int UserId { get; set; }

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
    /// Gets or sets the internal database primary key (IDENTITY/SERIAL/AUTOINCREMENT).
    /// This is NOT exposed to API consumers for security reasons.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the public-facing unique identifier exposed via APIs.
    /// Used to prevent IDOR and enumeration attacks. Generated on creation.
    /// </summary>
    [Required]
    public Guid PublicId { get; set; } = Guid.NewGuid();

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
    /// Gets or sets the user who performed the action (internal foreign key).
    /// ⚠️ SECURITY: This is the internal int FK for database efficiency.
    /// API consumers should never see this value - use PublicId instead.
    /// </summary>
    [Required]
    public int UserId { get; set; }

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
