using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.ViewModels
{
    public class SplashViewModel
    {
        private readonly Services.IAuthService _authService;

        public bool IsAuthenticated { get; private set; }

        // Provide a Login viewmodel for navigation convenience
        public LoginViewModel AuthViewModel { get; }

        public SplashViewModel(Services.IAuthService authService)
        {
            _authService = authService;
            AuthViewModel = new LoginViewModel(_authService);
        }

        public async Task InitializeAsync()
        {
            var token = await _authService.GetAccessTokenAsync();
            IsAuthenticated = !string.IsNullOrEmpty(token);
        }
    }
}
