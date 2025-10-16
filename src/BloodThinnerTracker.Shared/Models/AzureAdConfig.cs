// BloodThinnerTracker.Shared - Azure AD Configuration Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Azure AD configuration for enterprise authentication.
    /// </summary>
    public class AzureAdConfig
    {
        /// <summary>
        /// Gets or sets the Azure AD instance (e.g., https://login.microsoftonline.com/).
        /// </summary>
        [Required]
        public string Instance { get; set; } = "https://login.microsoftonline.com/";

        /// <summary>
        /// Gets or sets the Azure AD tenant ID.
        /// </summary>
        [Required]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the application (client) ID registered in Azure AD.
        /// </summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client secret for server-to-server authentication.
        /// </summary>
        [Required]
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback path for Azure AD authentication.
        /// </summary>
        public string CallbackPath { get; set; } = "/signin-oidc";

        /// <summary>
        /// Gets or sets the scopes required for medical data access.
        /// </summary>
        public List<string> Scopes { get; set; } = new () { "openid", "profile", "email" };
    }
}