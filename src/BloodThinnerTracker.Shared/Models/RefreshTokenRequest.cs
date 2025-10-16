// BloodThinnerTracker.Shared - Refresh Token Request Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Token refresh request model.
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Gets or sets the refresh token to exchange for new access token.
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the device identifier for security validation.
        /// </summary>
        public string? DeviceId { get; set; }
    }
}