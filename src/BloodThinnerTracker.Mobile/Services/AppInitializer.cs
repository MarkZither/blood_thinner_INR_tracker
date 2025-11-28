using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Services
{
    public class AppInitializer : IAppInitializer
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AppInitializer> _logger;
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly Microsoft.Extensions.Options.IOptions<FeaturesOptions> _features;
        private readonly IOAuthConfigService _oauthConfigService;

        public AppInitializer(IAuthService authService, ILogger<AppInitializer> logger,
            System.Net.Http.HttpClient httpClient,
            Microsoft.Extensions.Options.IOptions<FeaturesOptions> features,
            IOAuthConfigService oauthConfigService)
        {
            _authService = authService;
            _logger = logger;
            _httpClient = httpClient;
            _features = features;
            _oauthConfigService = oauthConfigService;
        }

        public async Task InitializeAsync(TimeSpan timeout)
        {
            try
            {
                _logger.LogDebug("AppInitializer: starting initialization with timeout {Timeout}ms", timeout.TotalMilliseconds);

                // Warm auth token
                var authTask = _authService.GetAccessTokenAsync();

                // Kick off remote config fetch if enabled
                Task? remoteTask = null;
                if (_features?.Value != null && _features.Value is FeaturesOptions fo && fo.GetType() != null)
                {
                    try
                    {
                        var enabled = fo.GetType().GetProperty("RemoteConfigEnabled")?.GetValue(fo) as bool? ?? false;
                        var url = fo.GetType().GetProperty("RemoteConfigUrl")?.GetValue(fo) as string;
                        if (enabled && !string.IsNullOrEmpty(url))
                        {
                            remoteTask = FetchRemoteConfigAsync(url, timeout);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "AppInitializer: failed to evaluate remote config feature flags");
                    }
                }

                // Wait for auth or timeout
                var completed = await Task.WhenAny(authTask, Task.Delay(timeout));
                if (completed == authTask)
                {
                    var token = await authTask;
                    _logger.LogDebug("AppInitializer: access token warm completed (has token: {HasToken})", !string.IsNullOrEmpty(token));
                }
                else
                {
                    _logger.LogWarning("AppInitializer: initialization timed out after {Timeout}ms", timeout.TotalMilliseconds);
                }

                // Also attempt to warm OAuth provider config (non-blocking, within timeout)
                Task? oauthTask = null;
                try
                {
                    oauthTask = _oauthConfigService.GetConfigAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "AppInitializer: failed to start OAuth config fetch");
                }

                // If remote config running, wait up to remaining time
                if (remoteTask != null || oauthTask != null)
                {
                    try
                    {
                        var remaining = timeout;
                        var tasks = new List<Task>();
                        if (remoteTask != null) tasks.Add(remoteTask);
                        if (oauthTask != null) tasks.Add(oauthTask);

                        if (tasks.Count > 0)
                        {
                            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(remaining));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "AppInitializer: remote config fetch failed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AppInitializer: exception during initialization");
            }
        }

        private async Task FetchRemoteConfigAsync(string url, TimeSpan timeout)
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(timeout);
                var client = _httpClient;
                _logger.LogDebug("AppInitializer: fetching remote config from {Url}", url);
                var resp = await client.GetAsync(url, cts.Token);
                if (resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync(cts.Token);
                    _logger.LogInformation("AppInitializer: remote config fetched ({Len} bytes)", body?.Length ?? 0);
                    // TODO: parse and apply remote config into local configuration or feature flags.
                }
                else
                {
                    _logger.LogWarning("AppInitializer: remote config fetch returned {Status}", resp.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AppInitializer: exception while fetching remote config");
            }
        }
    }
}
