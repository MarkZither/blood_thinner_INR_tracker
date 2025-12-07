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
        /// Perform a single sync: fetch recent INR data and persist to local DB.
        /// </summary>
        public async Task SyncOnceAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("CacheSyncService: starting sync");

                // 1) INR tests (persist into local DB)
                var inrResults = await _apiClient.GetRecentInrTestsAsync(50, cancellationToken).ConfigureAwait(false);
                if (inrResults != null)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    // Map API model -> InrListItemVm
                    var items = new System.Collections.Generic.List<ViewModels.InrListItemVm>();
                    foreach (var r in inrResults)
                    {
                        // Safely map nullable/generated types to non-nullable view model
                        var publicId = r.PublicId ?? Guid.Empty;
                        var testDate = r.TestDate?.DateTime ?? DateTime.MinValue;

                        // The generated model exposes InrValue as an UntypedNode; attempt a best-effort parse.
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
                        catch { /* best-effort only */ }

                        items.Add(new ViewModels.InrListItemVm
                        {
                            PublicId = publicId,
                            TestDate = testDate,
                            InrValue = inrDecimal,
                            Notes = r.Notes,
                            ReviewedByProvider = r.ReviewedByProvider ?? false
                        });
                    }

                    await _inrRepository.SaveRangeAsync(items).ConfigureAwait(false);
                    _logger.LogInformation("CacheSyncService: persisted {Count} INR items into local DB", items.Count);
                }

                // 2) Medications (cache for now; DB upsert will be implemented in Phase 2)
                var meds = await _apiClient.GetMedicationsAsync(cancellationToken).ConfigureAwait(false);
                if (meds != null)
                {
                    try
                    {
                        // Persist medications into local DB
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

                // 3) Medication logs (cache for now; DB upsert deferred)
                // The API exposes medication logs per-medication, so fetch logs per medication
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
                            var logs = await _apiClient.GetMedicationLogsAsync(medId, cancellationToken).ConfigureAwait(false);
                            if (logs != null) allLogs.AddRange(logs);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "CacheSyncService: failed to fetch logs for medication {MedicationId}", med.PublicId);
                        }
                    }

                    try
                    {
                        // Persist medication logs to DB
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
            }
            catch (ApiAuthenticationException aex)
            {
                // Authentication failures are expected when tokens expire; log succinctly and stop syncs.
                _logger.LogInformation(aex, "CacheSyncService: authentication required during sync - will stop until user re-authenticates");
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
