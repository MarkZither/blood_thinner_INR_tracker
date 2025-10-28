// BloodThinnerTracker.Shared - External Login Request Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// External OAuth2 login request model for medical application.
    /// Supports Azure AD and Google authentication providers.
    /// </summary>
    public class ExternalLoginRequest
    {
        /// <summary>
        /// Gets or sets the OAuth2 provider name (AzureAD or Google).
        /// </summary>
        [Required]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID token from the OAuth2 provider.
        /// For mobile apps: Platform-native OAuth returns ID token
        /// For web apps: Callback endpoint extracts from authorization code
        /// </summary>
        [Required]
        public string IdToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the device identifier for medical security tracking.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device platform (iOS, Android, Web).
        /// </summary>
        public string? DevicePlatform { get; set; }
    }
}
