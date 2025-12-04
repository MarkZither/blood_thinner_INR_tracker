using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BloodThinnerTracker.Mobile.Services;
using System.Net;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.ViewModels
{
    /// <summary>
    /// ViewModel for INR logs list view.
    /// Manages INR test history with caching, stale detection, and status indicators.
    /// Uses IInrService to fetch INR data.
    /// </summary>
    public partial class InrListViewModel : ObservableObject
    {
        private readonly IInrService _inrService;
        private readonly IInrRepository? _inrRepository;
        private readonly BloodThinnerTracker.Mobile.Services.Telemetry.ITelemetryService? _telemetry;
        private readonly ILogger<InrListViewModel>? _logger;
        private DateTime _lastUpdateTime = DateTime.MinValue;

        [ObservableProperty]
        private DateTime lastUpdatedAt = DateTime.MinValue;

        [ObservableProperty]
        private ObservableCollection<InrListItemViewModel> inrLogs = new();

        [ObservableProperty]
        private bool isBusy = false;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private bool showError = false;

        [ObservableProperty]
        private bool showList = false;

        [ObservableProperty]
        private bool showEmpty = false;

        [ObservableProperty]
        private string lastUpdatedText = "Never updated";

        /// <summary>
        /// Warning banner for stale cache (age > 1 hour).
        /// </summary>
        [ObservableProperty]
        private bool showStaleWarning = false;

        [ObservableProperty]
        private string staleWarningText = "Data may be outdated";

        /// <summary>
        /// Indicates data is from cache (offline or stale).
        /// </summary>
        [ObservableProperty]
        private bool isOfflineMode = false;

        [ObservableProperty]
        private string offlineModeText = "Offline - showing cached data";

        public InrListViewModel(IInrService inrService, IInrRepository? inrRepository = null, BloodThinnerTracker.Mobile.Services.Telemetry.ITelemetryService? telemetry = null, ILogger<InrListViewModel>? logger = null)
        {
            _inrService = inrService ?? throw new ArgumentNullException(nameof(inrService));
            _inrRepository = inrRepository;
            _telemetry = telemetry;
            _logger = logger;
        }

        /// <summary>
        /// Load INR logs from service with cache integration.
        /// Falls back to cached data if network unavailable.
        /// Shows stale warning if cache age > 1 hour.
        /// Called when view appears or refresh button clicked.
        /// </summary>
        [RelayCommand]
        public async Task LoadInrLogs()
        {
            if (IsBusy)
                return;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                IsBusy = true;
                ErrorMessage = null;
                ShowError = false;
                ShowList = false;
                ShowEmpty = false;
                ShowStaleWarning = false;
                IsOfflineMode = false;

                List<InrListItemVm> logsList = new();

                // Prefer reading from the local canonical DB when available (offline-first)
                if (_inrRepository != null)
                {
                    try
                    {
                        var local = await _inrRepository.GetRecentAsync(10);
                        if (local != null)
                        {
                            logsList = local.ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "InrRepository (local DB) failed while fetching recent INR tests");
                    }
                }

                // If no local items, fall back to remote service
                if (logsList.Count == 0)
                {
                    try
                    {
                        var logs = (await _inrService.GetRecentAsync(10))?.ToList() ?? new List<InrListItemVm>();

                        // If we have a local repository, persist the API results into the canonical
                        // local DB and then read back from the DB so the UI always shows the
                        // canonical, persisted INRTests (avoids inconsistencies between in-memory
                        // objects and DB state and ensures downstream code relying on DB works).
                        if (logs.Count > 0 && _inrRepository != null)
                        {
                            try
                            {
                                await _inrRepository.SaveRangeAsync(logs);

                                // Read back the most recent items from the DB to present canonical data
                                var persisted = await _inrRepository.GetRecentAsync(10);
                                logsList = persisted?.ToList() ?? new List<InrListItemVm>();
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "Failed to persist API INR results to local DB; falling back to in-memory results");
                                // Fall back to the API items if persistence fails
                                logsList = logs;
                            }
                        }
                        else
                        {
                            // No local repository available or no items returned; show API results in-memory
                            logsList = logs;
                        }
                    }
                    catch (ApiAuthenticationException ex)
                    {
                        // Surface a friendly UI error and navigate to login. When there are no
                        // local records and API returns 401, refreshing will always fail until
                        // the user re-authenticates, so proactively send them to the login page.
                        _logger?.LogInformation(ex, "Authentication required when fetching INR logs (navigating to login)");

                        ErrorMessage = "Session expired — please sign in again.";
                        ShowError = true;

                        try
                        {
                            if (Shell.Current != null)
                            {
                                await Shell.Current.GoToAsync("///login");
                            }
                        }
                        catch (Exception navEx)
                        {
                            _logger?.LogWarning(navEx, "Failed to navigate to login after authentication failure");
                        }

                        return;
                    }
                }

                if (logsList.Count > 0)
                {
                    // Display the logs (canonical data is in DB, no JSON cache)
                    var items = logsList.Select(log => new InrListItemViewModel(log)).ToList();
                    InrLogs = new ObservableCollection<InrListItemViewModel>(items);
                    ShowList = true;
                    _lastUpdateTime = DateTime.Now;
                    LastUpdatedAt = _lastUpdateTime;
                    UpdateLastUpdatedText();
                }
                else
                {
                    ShowEmpty = true;
                    InrLogs.Clear();
                }
            }
            catch (Exception ex)
            {
                // Network error or fetch failed
                ErrorMessage = $"Failed to fetch latest data: {ex.Message}";
                ShowError = true;
                _logger?.LogError(ex, "LoadInrLogs failed to fetch data");
            }
            finally
            {
                IsBusy = false;
                try
                {
                    sw.Stop();
                    _telemetry?.TrackHistogram("InrListLoadMs", sw.Elapsed.TotalMilliseconds);
                }
                catch (Exception tex)
                {
                    _logger?.LogDebug(tex, "Failed to track InrListLoadMs telemetry");
                }
            }
        }



        /// <summary>
        /// Update the "last updated" display text.
        /// Shows time elapsed since last fetch.
        /// </summary>
        private void UpdateLastUpdatedText()
        {
            if (_lastUpdateTime == DateTime.MinValue)
            {
                LastUpdatedText = "Never updated";
            }
            else
            {
                var elapsed = DateTime.Now - _lastUpdateTime;
                if (elapsed.TotalSeconds < 60)
                    LastUpdatedText = "Updated just now";
                else if (elapsed.TotalMinutes < 60)
                    LastUpdatedText = $"Updated {(int)elapsed.TotalMinutes}m ago";
                else if (elapsed.TotalHours < 24)
                    LastUpdatedText = $"Updated {(int)elapsed.TotalHours}h ago";
                else
                    LastUpdatedText = $"Updated {(int)elapsed.TotalDays}d ago";
            }
        }

        /// <summary>
        /// Navigate to add new INR entry (placeholder for future implementation).
        /// </summary>
        [RelayCommand]
        public async Task AddInr()
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("///about");
            }
        }
    }

    /// <summary>
    /// ViewModel wrapper for INR list items with computed properties for UI display.
    /// Includes status indicator, color coding, and INR value assessment.
    /// </summary>
    public class InrListItemViewModel
    {
        private readonly InrListItemVm _model;

        public Guid PublicId => _model.PublicId;
        public DateTime TestDate => _model.TestDate;
        public decimal InrValue => _model.InrValue;
        public string? Notes => _model.Notes;
        public bool ReviewedByProvider => _model.ReviewedByProvider;

        /// <summary>
        /// Status label based on INR value.
        /// Normal: 2.0 to 3.0, Elevated: above 3.0, Low: below 2.0
        /// </summary>
        public string StatusLabel
        {
            get
            {
                if (InrValue < 2.0m)
                    return "LOW";
                else if (InrValue > 3.0m)
                    return "ELEVATED";
                else
                    return "NORMAL";
            }
        }

        /// <summary>
        /// Status color for badge based on INR level.
        /// </summary>
        public Color StatusColor
        {
            get
            {
                return InrValue switch
                {
                    < 2.0m => Color.FromArgb("#DC3545"),  // Red for low
                    > 3.0m => Color.FromArgb("#FFC107"),  // Orange for elevated
                    _ => Color.FromArgb("#28A745")         // Green for normal
                };
            }
        }

        /// <summary>
        /// INR value color - emphasizes abnormal readings.
        /// </summary>
        public Color InrValueColor
        {
            get
            {
                return InrValue switch
                {
                    < 2.0m or > 3.0m => Color.FromArgb("#DC3545"),  // Red for out-of-range
                    _ => Color.FromArgb("#28A745")                   // Green for normal (opaque)
                };
            }
        }

        public InrListItemViewModel(InrListItemVm model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }
    }
}

