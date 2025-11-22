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

        public InrListViewModel(IInrService inrService)
        {
            _inrService = inrService ?? throw new ArgumentNullException(nameof(inrService));
        }

        /// <summary>
        /// Load INR logs from service and update UI states.
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

                var logs = await _inrService.GetRecentAsync(10);
                var logsList = logs?.ToList() ?? new List<InrListItemVm>();

                if (logsList.Count == 0)
                {
                    ShowEmpty = true;
                    InrLogs.Clear();
                }
                else
                {
                    // Convert to ViewModel items with calculated status
                    var items = logsList.Select(log => new InrListItemViewModel(log)).ToList();
                    InrLogs = new ObservableCollection<InrListItemViewModel>(items);
                    ShowList = true;
                }

                _lastUpdateTime = DateTime.Now;
                UpdateLastUpdatedText();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load INR data: {ex.Message}";
                ShowError = true;
                System.Diagnostics.Debug.WriteLine($"LoadInrLogs error: {ex}");
            }
            finally
            {
                IsBusy = false;
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

