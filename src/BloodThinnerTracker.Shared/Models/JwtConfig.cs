// BloodThinnerTracker.Shared - JWT Configuration Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// JWT token configuration for medical data security.
    /// </summary>
    public class JwtConfig
    {
        /// <summary>
        /// Gets or sets the JWT signing key (must be at least 256 bits for medical security).
        /// </summary>
        [Required]
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token issuer identifier.
        /// </summary>
        [Required]
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token audience identifier.
        /// </summary>
        [Required]
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the access token expiration in minutes (default: 15 minutes for medical security).
        /// </summary>
        public int AccessTokenExpirationMinutes { get; set; } = 15;

        /// <summary>
        /// Gets or sets the refresh token expiration in days (default: 7 days).
        /// </summary>
        public int RefreshTokenExpirationDays { get; set; } = 7;

        /// <summary>
        /// Gets or sets a value indicating whether HTTPS is required for all authentication operations.
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;
    }
}