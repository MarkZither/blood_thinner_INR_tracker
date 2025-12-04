namespace BloodThinnerTracker.Shared.Models.Authentication
{
    /// <summary>
    /// OAuth provider configuration sent to mobile clients.
    /// Contains client IDs, redirect URIs, and scopes for each provider.
    /// </summary>
    public class OAuthProviderConfig
    {
        /// <summary>
        /// Provider identifier (e.g., "azure", "google").
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// OAuth 2.0 client ID for this provider.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// OAuth 2.0 redirect URI (callback endpoint).
        /// For mobile: platform-specific redirect scheme or localhost.
        /// </summary>
        public string RedirectUri { get; set; } = string.Empty;

        /// <summary>
        /// OAuth 2.0 scopes to request (space-separated).
        /// Default: "openid profile email"
        /// </summary>
        public string Scopes { get; set; } = "openid profile email";

        /// <summary>
        /// OAuth authority endpoint (server where user authenticates).
        /// For Azure: https://login.microsoftonline.com/common
        /// For Google: https://accounts.google.com
        /// </summary>
        public string Authority { get; set; } = string.Empty;
    }

    /// <summary>
    /// OAuth configuration response sent to mobile clients.
    /// Contains configuration for all enabled OAuth providers.
    /// </summary>
    public class OAuthConfig
    {
        /// <summary>
        /// Timestamp when this configuration was generated (UTC).
        /// </summary>
        public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// List of configured OAuth providers (e.g., Azure AD, Google).
        /// </summary>
        public List<OAuthProviderConfig> Providers { get; set; } = new();
    }
}
