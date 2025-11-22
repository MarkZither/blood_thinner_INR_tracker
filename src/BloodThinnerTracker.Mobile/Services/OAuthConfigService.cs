using BloodThinnerTracker.Shared.Models.Authentication;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Service for fetching and caching OAuth provider configuration from the API.
    /// </summary>
    public class OAuthConfigService : IOAuthConfigService
    {
        private readonly HttpClient _httpClient;
        private OAuthConfig? _cachedConfig;
        private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;
        private const int CacheDurationMinutes = 60;

        public OAuthConfigService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Fetches OAuth configuration from the API, with in-memory caching.
        /// </summary>
        public async Task<OAuthConfig?> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            // Return cached config if still valid
            if (_cachedConfig != null && DateTimeOffset.UtcNow < _cacheExpiry)
            {
                return _cachedConfig;
            }

            try
            {
                var response = await _httpClient.GetAsync("api/auth/config", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var config = await response.Content.ReadAsAsync<OAuthConfig>(cancellationToken: cancellationToken);
                if (config == null)
                {
                    return null;
                }

                _cachedConfig = config;
                _cacheExpiry = DateTimeOffset.UtcNow.AddMinutes(CacheDurationMinutes);

                return config;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
