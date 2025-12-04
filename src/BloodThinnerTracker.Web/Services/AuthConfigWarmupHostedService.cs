using BloodThinnerTracker.Shared.Models.Authentication;
using BloodThinnerTracker.Shared.Constants;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace BloodThinnerTracker.Web.Services
{
    /// <summary>
    /// Hosted service that warms OAuth configuration from the API at startup and caches it in-memory.
    /// </summary>
    public class AuthConfigWarmupHostedService : IHostedService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<AuthConfigWarmupHostedService> _logger;
        private readonly IAuthConfigProvider _provider;
        private readonly IConfiguration _configuration;

        public AuthConfigWarmupHostedService(IHttpClientFactory httpFactory, ILogger<AuthConfigWarmupHostedService> logger, IAuthConfigProvider provider, IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _logger = logger;
            _provider = provider;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var apiBase = _configuration["ApiBaseUrl"] ?? _configuration["ApiRootUrl"] ?? "https://localhost:7234";
                var url = apiBase.TrimEnd('/') + ApiPaths.OAuthConfig;
                var client = _httpFactory.CreateClient();
                _logger.LogInformation("AuthConfigWarmup: fetching OAuth config from {Url}", url);
                var resp = await client.GetAsync(url, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AuthConfigWarmup: failed to fetch OAuth config ({Status})", resp.StatusCode);
                    return;
                }

                var config = await resp.Content.ReadFromJsonAsync<OAuthConfig>(cancellationToken: cancellationToken);
                if (config != null)
                {
                    _provider.SetCachedConfig(config);
                    _logger.LogInformation("AuthConfigWarmup: cached OAuth config with {Count} providers", config.Providers?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AuthConfigWarmup: exception while warming OAuth config");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
