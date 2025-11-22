using BloodThinnerTracker.Shared.Models.Authentication;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Service for fetching and caching OAuth provider configuration from the API.
    /// </summary>
    public class OAuthConfigService : IOAuthConfigService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OAuthConfigService> _logger;
        private OAuthConfig? _cachedConfig;
        private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;
        private const int CacheDurationMinutes = 60;

        public OAuthConfigService(HttpClient httpClient, ILogger<OAuthConfigService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Fetches OAuth configuration from the API, with in-memory caching.
        /// </summary>
        public async Task<OAuthConfig?> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            // Return cached config if still valid
            if (_cachedConfig != null && DateTimeOffset.UtcNow < _cacheExpiry)
            {
                _logger.LogDebug("Returning cached OAuth config (expires in {Minutes} minutes)",
                    (_cacheExpiry - DateTimeOffset.UtcNow).TotalMinutes);
                return _cachedConfig;
            }

            try
            {
                _logger.LogInformation("Fetching OAuth config from API");
                var response = await _httpClient.GetAsync("api/auth/config", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API returned status {StatusCode}: {ReasonPhrase}",
                        response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                var config = await response.Content.ReadFromJsonAsync<OAuthConfig>(cancellationToken: cancellationToken);
                if (config == null)
                {
                    _logger.LogWarning("API returned empty OAuth config");
                    return null;
                }

                _logger.LogInformation("Successfully fetched OAuth config from API with {ProviderCount} providers",
                    config.Providers?.Count ?? 0);

                _cachedConfig = config;
                _cacheExpiry = DateTimeOffset.UtcNow.AddMinutes(CacheDurationMinutes);

                return config;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching OAuth config: {Message}", ex.Message);
                return null;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "OAuth config fetch was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching OAuth config: {Message}", ex.Message);
                return null;
            }
        }
    }
}
