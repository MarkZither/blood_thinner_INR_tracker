using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.ViewModels
{
    /// <summary>
    /// ViewModel for INR logs list view.
    /// Manages INR test history and log entry creation.
    /// Uses IInrService to fetch/create INR data.
    /// </summary>
    public partial class InrListViewModel : ObservableObject
    {
        private readonly IInrService _inrService;

        [ObservableProperty]
        private ObservableCollection<InrListItemVm> inrLogs = new();

        [ObservableProperty]
        private bool isBusy = false;

        [ObservableProperty]
        private string? errorMessage;

        public InrListViewModel(IInrService inrService)
        {
            _inrService = inrService ?? throw new ArgumentNullException(nameof(inrService));
        }

        /// <summary>
        /// Load INR logs from service.
        /// Called when view appears.
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

                var logs = await _inrService.GetRecentAsync(10);
                InrLogs = new ObservableCollection<InrListItemVm>(logs);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading INR logs: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"LoadInrLogs error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Placeholder for adding new INR log entry.
        /// Will navigate to INR entry form in future.
        /// </summary>
        [RelayCommand]
        public async Task AddInr()
        {
            // TODO: Navigate to INR entry form
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("///about");
            }
        }
    }
}
