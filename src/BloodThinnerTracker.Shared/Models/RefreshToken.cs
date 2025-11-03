// BloodThinnerTracker.Shared - Refresh Token Entity for JWT Security
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Refresh token entity for secure token management.
    /// Implements NFR-002 session management with 7-day timeout.
    /// </summary>
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        /// <summary>
        /// Gets or sets the unique identifier for this refresh token.
        /// </summary>
        [Key]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the user ID who owns this token (internal foreign key).
        /// ⚠️ SECURITY: This is the internal int FK for database efficiency.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the actual refresh token value (hashed).
        /// </summary>
        [Required]
        [StringLength(500)]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the device ID that requested this token.
        /// </summary>
        [StringLength(100)]
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets when this token was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when this token expires (7 days per NFR-002).
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

        /// <summary>
        /// Gets or sets when this token was revoked (null if still valid).
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the reason for revocation.
        /// </summary>
        [StringLength(200)]
        public string? RevocationReason { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this token is revoked.
        /// </summary>
        public bool IsRevoked => RevokedAt.HasValue;

        /// <summary>
        /// Gets or sets a value indicating whether this token has expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// Gets or sets a value indicating whether this token is active (not revoked and not expired).
        /// </summary>
        public bool IsActive => !IsRevoked && !IsExpired;

        /// <summary>
        /// Navigation property to User.
        /// </summary>
        public virtual User? User { get; set; }
    }
}
