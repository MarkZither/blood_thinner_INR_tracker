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
        private readonly InrListViewModel _viewModel;

        // View is created via DI; view model is constructor-injected for testability.
        public InrListView(InrListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            BindingContext = _viewModel;
        }

        // Parameterless constructor used by XAML/DataTemplate instantiation.
        // Falls back to the application service provider to resolve the ViewModel.
        public InrListView()
            : this(App.ServiceProvider?.GetRequiredService<InrListViewModel>() ?? throw new InvalidOperationException("ServiceProvider is not initialized or InrListViewModel not registered."))
        {
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Load INR logs when view appears
            if (_viewModel.LoadInrLogsCommand.CanExecute(null))
            {
                await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);
            }
        }
    }

}
