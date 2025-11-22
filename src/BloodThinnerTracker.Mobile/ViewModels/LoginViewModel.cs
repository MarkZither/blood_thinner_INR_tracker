using System;
using System.Threading.Tasks;
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

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Initiates the OAuth sign-in flow.
        /// Returns true if successful; false otherwise.
        /// </summary>
        [RelayCommand]
        public async Task SignInAsync()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = null;

                // Step 1: Initiate OAuth flow to get id_token
                var idToken = await _authService.SignInAsync();
                if (string.IsNullOrEmpty(idToken))
                {
                    ErrorMessage = "Sign-in failed. Please check your credentials and try again.";
                    return;
                }

                // Step 2: Exchange id_token for internal bearer token
                var exchanged = await _authService.ExchangeIdTokenAsync(idToken);
                if (!exchanged)
                {
                    ErrorMessage = "Unable to complete sign-in. Please try again.";
                    return;
                }

                // Success - navigate away handled by caller
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

