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
        private readonly Lazy<InrListViewModel> _lazyViewModel;
        private readonly Microsoft.Extensions.Logging.ILogger<InrListView> _logger;
        private bool _viewModelBound = false;

        // Constructor receives a factory so the ViewModel creation can be deferred.
        public InrListView(BloodThinnerTracker.Mobile.Extensions.LazyViewModelFactory<InrListViewModel> factory, Microsoft.Extensions.Logging.ILogger<InrListView> logger)
        {
            InitializeComponent();
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _lazyViewModel = factory.CreateLazy();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Parameterless constructor used by XAML/DataTemplate instantiation.
        // Falls back to the application service provider to resolve the factory and logger.
        public InrListView()
            : this(
                  App.ServiceProvider?.GetRequiredService<BloodThinnerTracker.Mobile.Extensions.LazyViewModelFactory<InrListViewModel>>() ?? throw new InvalidOperationException("ServiceProvider is not initialized or LazyViewModelFactory<InrListViewModel> not registered."),
                  App.ServiceProvider?.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InrListView>>() ?? throw new InvalidOperationException("ServiceProvider is not initialized or ILogger<InrListView> not registered."))
        {
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _logger.LogDebug("InrListView appearing; ensuring ViewModel is initialized lazily.");

            // Bind the ViewModel lazily the first time the view appears
            if (!_viewModelBound)
            {
                try
                {
                    var vm = _lazyViewModel.Value;
                    BindingContext = vm;
                    _viewModelBound = true;

                    // Load INR logs when view appears
                    if (vm.LoadInrLogsCommand.CanExecute(null))
                    {
                        await vm.LoadInrLogsCommand.ExecuteAsync(null);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize or load ViewModel on appearing.");
                }
            }
        }
    }

}
