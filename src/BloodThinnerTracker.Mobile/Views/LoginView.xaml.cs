using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Views
{
    /// <summary>
    /// Login screen for OAuth authentication.
    /// Uses RelayCommand pattern from MVVM Toolkit for sign-in flow.
    /// Handles navigation to INR list view on successful login.
    /// </summary>
    public partial class LoginView : ContentPage
    {
        private readonly ViewModels.LoginViewModel _viewModel;

        public LoginView(ViewModels.LoginViewModel vm)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load XAML for LoginView: {ex.Message}", ex);
            }

            _viewModel = vm ?? throw new ArgumentNullException(nameof(vm));
            BindingContext = vm;

            // Subscribe to login success to handle navigation
            vm.LoginSucceeded += OnLoginSucceeded;
        }

        /// <summary>
        /// Parameterless constructor required for XAML DataTemplate instantiation.
        /// Shell's ContentTemplate uses Activator, not DI, so we bridge to DI here.
        /// This is a necessary workaround for MAUI Shell's DataTemplate limitation.
        /// </summary>
        public LoginView()
            : this(
                  App.ServiceProvider?.GetRequiredService<ViewModels.LoginViewModel>()
                      ?? throw new InvalidOperationException("ServiceProvider not initialized"))
        {
        }

        /// <summary>
        /// Navigate to INR list view on successful login.
        /// Uses Shell routing for navigation, switches to flyout navigation.
        /// </summary>
        private async void OnLoginSucceeded(object? sender, EventArgs e)
        {
            try
            {
                // Navigate to home flyout item which shows InrListView
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("///flyouthome/inrlist");
                }
            }
            catch (Exception ex)
            {
                var logger = App.ServiceProvider?.GetService<Microsoft.Extensions.Logging.ILogger<LoginView>>();
                logger?.LogWarning(ex, "Navigation error during OnLoginSucceeded");
                // Log error but don't crash - user stays on login screen
            }
        }
    }
}

