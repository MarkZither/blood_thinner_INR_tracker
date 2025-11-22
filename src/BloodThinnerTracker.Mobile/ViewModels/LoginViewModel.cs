using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.ViewModels
{
    /// <summary>
    /// ViewModel for the login screen.
    /// Handles OAuth sign-in flow and token exchange.
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? selectedProvider = "azure";

        /// <summary>
        /// Event raised when login succeeds, allowing the view to navigate.
        /// </summary>
        public event EventHandler? LoginSucceeded;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;

            // Initialize commands
            SignInWithAzureAsyncCommand = new AsyncRelayCommand(SignInWithAzureAsync);
            SignInWithGoogleAsyncCommand = new AsyncRelayCommand(SignInWithGoogleAsync);
        }

        /// <summary>
        /// Command for Azure AD sign-in (exposed for XAML binding).
        /// </summary>
        public ICommand SignInWithAzureAsyncCommand { get; }

        /// <summary>
        /// Command for Google sign-in (exposed for XAML binding).
        /// </summary>
        public ICommand SignInWithGoogleAsyncCommand { get; }

        /// <summary>
        /// Sign in with Azure AD provider.
        /// </summary>
        public async Task SignInWithAzureAsync()
        {
            await PerformSignInAsync("azure");
        }

        /// <summary>
        /// Sign in with Google provider.
        /// </summary>
        public async Task SignInWithGoogleAsync()
        {
            await PerformSignInAsync("google");
        }

        /// <summary>
        /// Internal method to perform sign-in with specified provider.
        /// </summary>
        private async Task PerformSignInAsync(string provider)
        {
            try
            {
                IsBusy = true;
                ErrorMessage = null;
                SelectedProvider = provider;

                // Step 1: Initiate OAuth flow to get id_token
                var idToken = await _authService.SignInAsync();
                if (string.IsNullOrEmpty(idToken))
                {
                    ErrorMessage = "Sign-in failed. Please check your credentials and try again.";
                    return;
                }

                // Step 2: Exchange id_token for internal bearer token
                var exchanged = await _authService.ExchangeIdTokenAsync(idToken, provider);
                if (!exchanged)
                {
                    ErrorMessage = "Unable to complete sign-in. Please try again.";
                    return;
                }

                // Success - raise event for view to handle navigation
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                // User cancelled sign-in flow
                ErrorMessage = "Sign-in cancelled.";
            }
            catch
            {
                ErrorMessage = "An unexpected error occurred. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

