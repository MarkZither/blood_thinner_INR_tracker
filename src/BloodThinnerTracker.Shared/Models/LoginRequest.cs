// BloodThinnerTracker.Shared - Login Request Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Login request model for medical application.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Gets or sets the user identifier (email or username).
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user password.
        /// </summary>
        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to remember the user for extended session.
        /// </summary>
        public bool RememberMe { get; set; } = false;

        /// <summary>
        /// Gets or sets the device identifier for medical security tracking.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the two-factor authentication code if required.
        /// </summary>
        public string? TwoFactorCode { get; set; }
    }
}