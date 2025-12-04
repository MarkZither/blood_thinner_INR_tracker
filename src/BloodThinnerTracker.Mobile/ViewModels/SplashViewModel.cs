using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.ViewModels
{
    public class SplashViewModel
    {
        private readonly IAuthService _auth;
        public bool IsAuthenticated { get; private set; }

        public SplashViewModel(IAuthService auth)
        {
            _auth = auth;
        }

        public async Task InitializeAsync()
        {
            var token = await _auth.GetAccessTokenAsync();
            IsAuthenticated = !string.IsNullOrEmpty(token);
        }
    }
}
