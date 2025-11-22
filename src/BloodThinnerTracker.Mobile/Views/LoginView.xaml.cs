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
        /// Uses shell routing for cross-app navigation.
        /// </summary>
        private async void OnLoginSucceeded(object? sender, EventArgs e)
        {
            try
            {
                // Use Shell routing to navigate to main content
                // Once InrListView is created, add it to AppShell routes
                // For now, navigate using a generic route that will be defined in AppShell
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("//inrlist");
                }
                else if (Navigation != null)
                {
                    // Fallback: Close this login view
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                // Silently fail - shell routing not configured yet
                // Once AppShell is implemented with inrlist route, this will work
            }
        }
    }
}

