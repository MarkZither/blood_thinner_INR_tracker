using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BloodThinnerTracker.Mobile.Config;

namespace BloodThinnerTracker.Mobile.Services
{
    public interface IForegroundSyncService
    {
        Task EnsureForegroundSyncAsync();
    }

    public class ForegroundSyncService : IForegroundSyncService
    {
        private readonly ICacheService _cache;
        private readonly ICacheSyncWorker _syncWorker;
        private readonly SyncOptions _options;
        private readonly ILogger<ForegroundSyncService> _logger;

        public ForegroundSyncService(ICacheService cache, ICacheSyncWorker syncWorker, IOptions<SyncOptions> options, ILogger<ForegroundSyncService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _syncWorker = syncWorker ?? throw new ArgumentNullException(nameof(syncWorker));
            _options = options?.Value ?? new SyncOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task EnsureForegroundSyncAsync()
        {
            try
            {
                var last = await _cache.GetAsync("last_sync_utc").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(last) && DateTime.TryParse(last, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                {
                    var age = DateTime.UtcNow - dt;
                    var threshold = TimeSpan.FromMinutes(_options.DefaultIntervalMinutes > 0 ? _options.DefaultIntervalMinutes : 60);
                    if (age < threshold)
                    {
                        _logger.LogDebug("ForegroundSyncService: last sync {Age} ago which is less than threshold {Threshold}, skipping foreground sync", age, threshold);
                        return;
                    }
                }

                _logger.LogDebug("ForegroundSyncService: triggering foreground sync");
                await _syncWorker.SyncOnceAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ForegroundSyncService: foreground sync encountered an error");
            }
        }
    }
}
