using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BloodThinnerTracker.Mobile.Services;

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
        private readonly ICacheService _cacheService;
        private DateTime _lastUpdateTime = DateTime.MinValue;

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

        // Cache key for INR logs
        private const string InrLogsCache = "inr_logs";

        public InrListViewModel(IInrService inrService, ICacheService cacheService)
        {
            _inrService = inrService ?? throw new ArgumentNullException(nameof(inrService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
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

            try
            {
                IsBusy = true;
                ErrorMessage = null;
                ShowError = false;
                ShowList = false;
                ShowEmpty = false;
                ShowStaleWarning = false;
                IsOfflineMode = false;

                var logs = await _inrService.GetRecentAsync(10);
                var logsList = logs?.ToList() ?? new List<InrListItemVm>();

                if (logsList.Count > 0)
                {
                    // Store in cache for offline access
                    var logsJson = System.Text.Json.JsonSerializer.Serialize(logsList);
                    await _cacheService.SetAsync(InrLogsCache, logsJson);

                    // Display the logs
                    var items = logsList.Select(log => new InrListItemViewModel(log)).ToList();
                    InrLogs = new ObservableCollection<InrListItemViewModel>(items);
                    ShowList = true;
                    _lastUpdateTime = DateTime.Now;
                    UpdateLastUpdatedText();
                }
                else
                {
                    // No new data - check if we have cached data to fallback to
                    await TryLoadFromCacheAsync();

                    if (!ShowList)
                    {
                        ShowEmpty = true;
                        InrLogs.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                // Network error or fetch failed - try to show cached data
                ErrorMessage = $"Failed to fetch latest data: {ex.Message}";
                await TryLoadFromCacheAsync();

                if (!ShowList)
                {
                    // No cache available either
                    ShowError = true;
                    System.Diagnostics.Debug.WriteLine($"LoadInrLogs error: {ex}");
                }
                else
                {
                    // Showed cached data but with error message
                    ShowStaleWarning = true;
                    StaleWarningText = $"Error fetching latest data. Showing cached data.";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Try to load INR logs from cache and display them.
        /// Also check staleness and show warnings if needed.
        /// </summary>
        private async Task TryLoadFromCacheAsync()
        {
            try
            {
                var cachedJson = await _cacheService.GetAsync(InrLogsCache);
                if (string.IsNullOrEmpty(cachedJson))
                    return;

                // Deserialize cached logs
                var cachedLogs = System.Text.Json.JsonSerializer.Deserialize<List<InrListItemVm>>(cachedJson);
                if (cachedLogs == null || cachedLogs.Count == 0)
                    return;

                // Check cache age for staleness warning
                var cacheAgeMs = await _cacheService.GetCacheAgeMillisecondsAsync(InrLogsCache);
                if (cacheAgeMs.HasValue)
                {
                    var cacheAgeHours = cacheAgeMs.Value / 1000.0 / 60.0 / 60.0;
                    if (cacheAgeHours > 1)
                    {
                        ShowStaleWarning = true;
                        StaleWarningText = $"Showing cached data from {cacheAgeHours:F1} hours ago";
                    }
                }

                // Display cached logs
                var items = cachedLogs.Select(log => new InrListItemViewModel(log)).ToList();
                InrLogs = new ObservableCollection<InrListItemViewModel>(items);
                ShowList = true;
                IsOfflineMode = true;
                OfflineModeText = "⚠ Offline - showing cached data";
                _lastUpdateTime = DateTime.Now;
                UpdateLastUpdatedText();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryLoadFromCacheAsync error: {ex.Message}");
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

