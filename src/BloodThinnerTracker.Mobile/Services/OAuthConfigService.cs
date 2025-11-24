using BloodThinnerTracker.Shared.Models.Authentication;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Service for fetching and caching OAuth provider configuration from the API.
    /// Uses Flurl for clean URL composition.
    /// </summary>
    public class OAuthConfigService : IOAuthConfigService
    {
        private readonly IOptions<FeaturesOptions> _featuresOptions;
        private readonly HttpClient _httpClient;
        private readonly ILogger<OAuthConfigService> _logger;
        private OAuthConfig? _cachedConfig;
        private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;
        private const int CacheDurationMinutes = 60;

        public OAuthConfigService(IOptions<FeaturesOptions> featuresOptions, HttpClient httpClient, ILogger<OAuthConfigService> logger)
        {
            _featuresOptions = featuresOptions;
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

                // Get API root URL from strongly-typed options (no magic strings!)
                var apiRootUrl = _featuresOptions.Value.ApiRootUrl;
                if (string.IsNullOrEmpty(apiRootUrl) || apiRootUrl == "https://api.example.invalid")
                {
                    _logger.LogError("Features:ApiRootUrl is not configured or is using default placeholder");
                    return null;
                }

                // Build URL: https://localhost:7234/api/oauth/config
                var url = $"{apiRootUrl.TrimEnd('/')}/{ApiConstants.OAuthConfigPath}";
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch OAuth config with status {response.StatusCode}");
                    return null;
                }

                var config = await response.Content.ReadFromJsonAsync<OAuthConfig>();
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
