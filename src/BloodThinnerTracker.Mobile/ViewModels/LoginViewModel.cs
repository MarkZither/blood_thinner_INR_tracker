using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.ViewModels
{
    public class LoginViewModel
    {
        private readonly Services.IAuthService _authService;

        public LoginViewModel(Services.IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<bool> SignInAsync()
        {
            // Start platform sign-in (stub) and exchange id_token for internal token
            var id = await _authService.SignInAsync();
            if (string.IsNullOrEmpty(id)) return false;
            return await _authService.ExchangeIdTokenAsync(id);
        }
    }
}
