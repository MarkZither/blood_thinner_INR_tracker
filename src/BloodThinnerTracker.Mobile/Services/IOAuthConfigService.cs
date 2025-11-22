using BloodThinnerTracker.Shared.Models.Authentication;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Service for fetching and caching OAuth provider configuration from the API.
    /// </summary>
    public interface IOAuthConfigService
    {
        /// <summary>
        /// Fetches OAuth configuration from the API.
        /// Includes client IDs, redirect URIs, and scopes for each provider.
        /// </summary>
        /// <returns>OAuth configuration if successful; null if fetch failed.</returns>
        Task<OAuthConfig?> GetConfigAsync(CancellationToken cancellationToken = default);
    }
}
