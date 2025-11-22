using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using BloodThinnerTracker.Mobile.ViewModels;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.Views
{
    /// <summary>
    /// INR test logs list view.
    /// Displays history of INR tests and allows logging new entries.
    /// Requires authenticated user (shown after successful login).
    /// </summary>
    public partial class InrListView : ContentPage
    {
        private InrListViewModel? _viewModel;

        public InrListView()
        {
            InitializeComponent();
            // ViewModel created lazily in OnAppearing to avoid premature service initialization
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Create ViewModel lazily on first appearance to ensure auth is complete
            if (_viewModel == null)
            {
                var inrService = ServiceHelper.Current?.GetRequiredService<IInrService>();
                if (inrService == null)
                    return;

                _viewModel = new InrListViewModel(inrService);
                BindingContext = _viewModel;
            }

            // Load INR logs when view appears
            if (_viewModel?.LoadInrLogsCommand.CanExecute(null) == true)
            {
                await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);
            }
        }
    }

    /// <summary>
    /// Helper to access IServiceProvider from anywhere in the app.
    /// </summary>
    public static class ServiceHelper
    {
        public static IServiceProvider? Current { get; set; }
    }
}
