using BloodThinnerTracker.Shared.Models.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Web.Services
{
    /// <summary>
    /// In-memory provider for OAuth configuration fetched from API.
    /// Other Web services can request the last-known configuration from here.
    /// </summary>
    public interface IAuthConfigProvider
    {
        OAuthConfig? GetCachedConfig();
        void SetCachedConfig(OAuthConfig config);
    }

    public class AuthConfigProvider : IAuthConfigProvider
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<AuthConfigProvider> _logger;
        private const string CacheKey = "oauth_config_web";

        public AuthConfigProvider(IMemoryCache cache, ILogger<AuthConfigProvider> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public OAuthConfig? GetCachedConfig()
        {
            if (_cache.TryGetValue(CacheKey, out OAuthConfig? cfg))
                return cfg;
            return null;
        }

        public void SetCachedConfig(OAuthConfig config)
        {
            var opts = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };
            _cache.Set(CacheKey, config, opts);
            _logger.LogInformation("AuthConfigProvider: cached OAuth config with {ProviderCount} providers", config.Providers?.Count ?? 0);
        }
    }
}
