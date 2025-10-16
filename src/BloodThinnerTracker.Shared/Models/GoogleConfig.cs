// BloodThinnerTracker.Shared - Google OAuth Configuration Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Google OAuth configuration for consumer authentication.
    /// </summary>
    public class GoogleConfig
    {
        /// <summary>
        /// Gets or sets the Google OAuth client ID.
        /// </summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Google OAuth client secret.
        /// </summary>
        [Required]
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback path for Google OAuth.
        /// </summary>
        public string CallbackPath { get; set; } = "/signin-google";

        /// <summary>
        /// Gets or sets the scopes required for medical data access.
        /// </summary>
        public List<string> Scopes { get; set; } = new () { "openid", "profile", "email" };
    }
}