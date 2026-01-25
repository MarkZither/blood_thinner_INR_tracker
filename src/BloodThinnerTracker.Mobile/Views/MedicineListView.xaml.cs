using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BloodThinnerTracker.Mobile.ViewModels;

namespace BloodThinnerTracker.Mobile.Views
{
    public partial class MedicineListView : ContentPage
    {
        private readonly Lazy<MedicineListViewModel> _lazyViewModel;
        private readonly ILogger<MedicineListView> _logger;
        private bool _viewModelBound = false;

        public MedicineListView(BloodThinnerTracker.Mobile.Extensions.LazyViewModelFactory<MedicineListViewModel> factory, ILogger<MedicineListView> logger)
        {
            try { InitializeComponent(); }
            catch (Exception ex) { throw new InvalidOperationException($"Failed to load XAML for MedicineListView: {ex.Message}", ex); }

            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _lazyViewModel = factory.CreateLazy();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public MedicineListView()
            : this(
                  App.ServiceProvider?.GetRequiredService<BloodThinnerTracker.Mobile.Extensions.LazyViewModelFactory<MedicineListViewModel>>() ?? throw new InvalidOperationException("ServiceProvider not initialized"),
                  App.ServiceProvider?.GetRequiredService<ILogger<MedicineListView>>() ?? throw new InvalidOperationException("ILogger<MedicineListView> not registered"))
        {
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_viewModelBound)
            {
                try
                {
                    var vm = _lazyViewModel.Value;
                    BindingContext = vm;
                    _viewModelBound = true;

                    if (vm.LoadMedicationsCommand.CanExecute(null))
                    {
                        await vm.LoadMedicationsCommand.ExecuteAsync(null);
                    }

                    try
                    {
                        var fg = App.ServiceProvider?.GetService<Services.IForegroundSyncService>();
                        if (fg != null)
                        {
                            _ = fg.EnsureForegroundSyncAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Foreground sync trigger failed on MedicineListView appearing");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize or load MedicineListViewModel on appearing.");
                }
            }
        }
    }
}
