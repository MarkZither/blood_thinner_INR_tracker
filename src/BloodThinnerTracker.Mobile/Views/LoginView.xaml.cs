using Microsoft.Maui.Controls;

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
            InitializeComponent();
            _viewModel = vm;
            BindingContext = vm;

            // Subscribe to login success to handle navigation
            vm.LoginSucceeded += OnLoginSucceeded;
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
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                // Log error but don't crash - user stays on login screen
            }
        }
    }
}

