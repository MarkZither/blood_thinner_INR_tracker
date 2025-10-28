// BloodThinnerTracker.Shared - Authentication Response Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Authentication response with JWT tokens.
    /// </summary>
    public class AuthenticationResponse
    {
        /// <summary>
        /// Gets or sets the JWT access token for API access.
        /// </summary>
        [Required]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the refresh token for token renewal.
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token type (typically "Bearer").
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Gets or sets the access token expiration in seconds.
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the user information.
        /// </summary>
        public UserInfo User { get; set; } = new ();

        /// <summary>
        /// Gets or sets the medical data access permissions.
        /// </summary>
        public List<string> Permissions { get; set; } = new ();

        /// <summary>
        /// Gets or sets a value indicating whether multi-factor authentication is required.
        /// </summary>
        public bool RequiresMfa { get; set; }
    }
}