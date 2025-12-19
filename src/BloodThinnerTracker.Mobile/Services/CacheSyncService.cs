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
        // Prevent overlapping sync executions
        private readonly System.Threading.SemaphoreSlim _syncLock = new(1, 1);
        private readonly IApiClientFacade _apiClient;
        private readonly IInrRepository _inrRepository;
        private readonly ICacheService _cacheService;
        private readonly IMedicationRepository _medicationRepository;
        private readonly IMedicationLogRepository _medicationLogRepository;
        private readonly ILogger<CacheSyncService> _logger;
        private readonly TimeSpan _interval;
        private Timer? _timer;

        // Default sync interval (fallback)
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(60);

        public CacheSyncService(IApiClientFacade apiClient, IInrRepository inrRepository, ICacheService cacheService, IMedicationRepository medicationRepository, IMedicationLogRepository medicationLogRepository, Microsoft.Extensions.Options.IOptions<Config.SyncOptions> syncOptions, ILogger<CacheSyncService> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _inrRepository = inrRepository ?? throw new ArgumentNullException(nameof(inrRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _medicationRepository = medicationRepository ?? throw new ArgumentNullException(nameof(medicationRepository));
            _medicationLogRepository = medicationLogRepository ?? throw new ArgumentNullException(nameof(medicationLogRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var opts = syncOptions?.Value ?? new Config.SyncOptions();
            _interval = TimeSpan.FromMinutes(opts.DefaultIntervalMinutes > 0 ? opts.DefaultIntervalMinutes : 60);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("CacheSyncService starting (interval={Interval})", _interval);

            // Start an immediate run then periodic timer using configured interval
            _timer = new Timer(async _ => await SafeSyncAsync(), null, TimeSpan.Zero, _interval);

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
        /// Perform a single sync: fetch recent INR data, medications and medication logs, and persist to local DB.
        /// Extracted into a public method so it can be called directly for tests or foreground sync.
        /// </summary>
        public async Task FetchAndUpsertAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("CacheSyncService: FetchAndUpsertAsync starting");

            // 1) INR tests (persist into local DB)
            var inrResults = await ExecuteWithRetriesAsync(() => _apiClient.GetRecentInrTestsAsync(50, cancellationToken), maxAttempts: 3).ConfigureAwait(false);
            if (inrResults != null)
            {
                if (cancellationToken.IsCancellationRequested) return;
                var items = new System.Collections.Generic.List<ViewModels.InrListItemVm>();
                foreach (var r in inrResults)
                {
                    var publicId = r.PublicId ?? Guid.Empty;
                    var testDate = r.TestDate?.DateTime ?? DateTime.MinValue;

                    decimal inrDecimal = 0m;
                    try
                    {
                        if (r.InrValue != null)
                        {
                            var text = r.InrValue.ToString();
                            if (!string.IsNullOrEmpty(text) && decimal.TryParse(text, out var parsed))
                                inrDecimal = parsed;
                        }
                    }
                    catch { }

                    items.Add(new ViewModels.InrListItemVm
                    {
                        PublicId = publicId,
                        TestDate = testDate,
                        InrValue = inrDecimal,
                        Notes = r.Notes,
                        ReviewedByProvider = r.ReviewedByProvider ?? false
                    });
                }

                try
                {
                    await _inrRepository.SaveRangeAsync(items).ConfigureAwait(false);
                    _logger.LogInformation("CacheSyncService: persisted {Count} INR items into local DB", items.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "CacheSyncService: failed to persist INR items to DB, falling back to cache");
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(items);
                        await _cacheService.SetAsync("inr_tests", json).ConfigureAwait(false);
                        _logger.LogInformation("CacheSyncService: cached {Count} INR items", items.Count);
                    }
                    catch (Exception inner)
                    {
                        _logger.LogWarning(inner, "CacheSyncService: failed to cache INR items after DB persist failure");
                    }
                }
            }

            // 2) Medications
            var meds = await ExecuteWithRetriesAsync(() => _apiClient.GetMedicationsAsync(cancellationToken), maxAttempts: 3).ConfigureAwait(false);
            if (meds != null)
            {
                try
                {
                    await _medicationRepository.UpsertRangeAsync(meds, cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("CacheSyncService: upserted {Count} medications into local DB", meds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "CacheSyncService: failed to persist medications to DB, falling back to cache");
                    var json = System.Text.Json.JsonSerializer.Serialize(meds);
                    await _cacheService.SetAsync("medications", json).ConfigureAwait(false);
                    _logger.LogInformation("CacheSyncService: cached {Count} medications", meds.Count);
                }
            }

            // 3) Medication logs (per-medication)
            if (meds != null && meds.Count > 0)
            {
                var allLogs = new System.Collections.Generic.List<global::ApiSdk.Models.MedicationLogResponse>();
                foreach (var med in meds)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                        try
                        {
                            if (!med.PublicId.HasValue) continue;
                            var medId = med.PublicId.Value;
                            var logs = await ExecuteWithRetriesAsync(() => _apiClient.GetMedicationLogsAsync(medId, cancellationToken), maxAttempts: 3).ConfigureAwait(false);
                            if (logs != null) allLogs.AddRange(logs);
                        }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "CacheSyncService: failed to fetch logs for medication {MedicationId}", med.PublicId);
                    }
                }

                try
                {
                    await _medicationLogRepository.SaveRangeAsync(allLogs, cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("CacheSyncService: saved {Count} medication logs into local DB (from {MedicationCount} medications)", allLogs.Count, meds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "CacheSyncService: failed to persist medication logs to DB, falling back to cache");
                    var json = System.Text.Json.JsonSerializer.Serialize(allLogs);
                    await _cacheService.SetAsync("medication_logs", json).ConfigureAwait(false);
                    _logger.LogInformation("CacheSyncService: cached {Count} medication logs (from {MedicationCount} medications)", allLogs.Count, meds.Count);
                }
            }

            // Mark last sync time so foreground logic can make informed decisions
            try
            {
                await _cacheService.SetAsync("last_sync_utc", DateTime.UtcNow.ToString("o")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CacheSyncService: failed to set last sync cache key");
            }
        }

        // Expose configured interval for test verification
        internal TimeSpan ConfiguredInterval => _interval;

        public async Task SyncOnceAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            // Prevent overlapping syncs
            if (!_syncLock.Wait(0))
            {
                _logger.LogDebug("CacheSyncService: sync already running - skipping concurrent invocation");
                return;
            }

            try
            {
                _logger.LogDebug("CacheSyncService: starting sync");
                await FetchAndUpsertAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ApiAuthenticationException aex)
            {
                _logger.LogInformation(aex, "CacheSyncService: authentication required during sync - will stop until user re-authenticates");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CacheSyncService: sync once error");
            }
            finally
            {
                _syncLock.Release();
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

        // Simple retry helper for transient API calls. Uses exponential backoff and logs attempts.
        private async Task<T?> ExecuteWithRetriesAsync<T>(Func<Task<T?>> operation, int maxAttempts = 3, int initialDelayMs = 100)
            where T : class
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            var attempt = 0;
            var delay = initialDelayMs;
            while (true)
            {
                attempt++;
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogWarning(ex, "CacheSyncService: transient error on attempt {Attempt}/{MaxAttempts}, retrying after {Delay}ms", attempt, maxAttempts, delay);
                    await Task.Delay(delay).ConfigureAwait(false);
                    delay *= 2; // exponential backoff
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "CacheSyncService: operation failed after {Attempt} attempts", attempt);
                    return null;
                }
            }
        }
    }
}
