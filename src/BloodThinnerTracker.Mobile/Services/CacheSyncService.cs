using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Background hosted service that periodically syncs INR data from the remote API
    /// into the local encrypted cache so the app can operate offline.
    ///
    /// Runs on a timer and performs a best-effort sync; failures are logged but do not crash the app.
    /// </summary>
    public class CacheSyncService : IHostedService, IDisposable, ICacheSyncWorker
    {
        private readonly IInrService _inrService;
        private readonly IInrRepository _inrRepository;
        private readonly ILogger<CacheSyncService> _logger;
        private Timer? _timer;

        // Default sync interval (15 minutes)
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(15);

        public CacheSyncService(IInrService inrService, IInrRepository inrRepository, ILogger<CacheSyncService> logger)
        {
            _inrService = inrService ?? throw new ArgumentNullException(nameof(inrService));
            _inrRepository = inrRepository ?? throw new ArgumentNullException(nameof(inrRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("CacheSyncService starting");

            // Start an immediate run then periodic timer
            _timer = new Timer(async _ => await SafeSyncAsync(), null, TimeSpan.Zero, DefaultInterval);

            return Task.CompletedTask;
        }

        private async Task SafeSyncAsync()
        {
            try
            {
                await SyncOnceAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CacheSyncService: periodic sync failed");
            }
        }

        /// <summary>
        /// Perform a single sync: fetch recent INR data and persist to local DB.
        /// </summary>
        public async Task SyncOnceAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("CacheSyncService: starting sync");
                var items = await _inrService.GetRecentAsync(50);
                if (items != null)
                {
                    // Persist into the canonical local DB (DB-first approach)
                    // Honor cancellation cooperatively when saving large batches.
                    if (cancellationToken.IsCancellationRequested) return;
                    await _inrRepository.SaveRangeAsync(items);
                    _logger.LogInformation("CacheSyncService: persisted {Count} items into local DB", items is System.Collections.ICollection c ? c.Count : -1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CacheSyncService: sync once error");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("CacheSyncService stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
