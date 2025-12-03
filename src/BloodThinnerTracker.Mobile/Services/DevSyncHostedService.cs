using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Development-only hosted service that runs the same sync worker in-process
    /// so developers can iterate without packaging the app.
    /// It calls ICacheSyncWorker.SyncOnceAsync on a configurable interval.
    /// </summary>
    public class DevSyncHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<DevSyncHostedService> _logger;
        private readonly ICacheSyncWorker _syncWorker;
        private Timer? _timer;
        private readonly TimeSpan _interval;

        public DevSyncHostedService(ILogger<DevSyncHostedService> logger, ICacheSyncWorker syncWorker)
        {
            _logger = logger;
            _syncWorker = syncWorker;
            // Default to 15 minutes (match WinRT periodic task) but allow quick iteration
            _interval = TimeSpan.FromMinutes(3);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DevSyncHostedService starting (in-process dev worker)");

            // Run immediately once, then start periodic timer
            _ = RunOnceSafeAsync(CancellationToken.None);

            _timer = new Timer(async _ => await RunOnceSafeAsync(CancellationToken.None), null, _interval, _interval);
            return Task.CompletedTask;
        }

        private async Task RunOnceSafeAsync(CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("DevSyncHostedService invoking ICacheSyncWorker.SyncOnceAsync");
                await _syncWorker.SyncOnceAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("DevSyncHostedService sync completed");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("DevSyncHostedService cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DevSyncHostedService failed during sync");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DevSyncHostedService stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
