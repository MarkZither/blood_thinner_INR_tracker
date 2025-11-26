using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
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
        private readonly InrListViewModel _viewModel;
        private readonly Microsoft.Extensions.Logging.ILogger<InrListView> _logger;

        // View is created via DI; view model and logger are constructor-injected for testability.
        public InrListView(InrListViewModel viewModel, Microsoft.Extensions.Logging.ILogger<InrListView> logger)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            BindingContext = _viewModel;
        }

        // Parameterless constructor used by XAML/DataTemplate instantiation.
        // Falls back to the application service provider to resolve the ViewModel and logger.
        public InrListView()
            : this(
                  App.ServiceProvider?.GetRequiredService<InrListViewModel>() ?? throw new InvalidOperationException("ServiceProvider is not initialized or InrListViewModel not registered."),
                  App.ServiceProvider?.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InrListView>>() ?? throw new InvalidOperationException("ServiceProvider is not initialized or ILogger<InrListView> not registered."))
        {
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _logger.LogWarning("Testing logging a warning.");
            _logger.LogDebug("InrListView appearing; loading INR logs.");

            // Load INR logs when view appears
            if (_viewModel.LoadInrLogsCommand.CanExecute(null))
            {
                try
                {
                    await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load INR logs on appearing.");
                }
            }
        }
    }

}
