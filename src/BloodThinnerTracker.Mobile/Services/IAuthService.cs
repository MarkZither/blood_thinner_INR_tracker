using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Services
{
    public interface IAuthService
    {
        Task<string> SignInAsync();
        Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure");
        Task<string?> GetAccessTokenAsync();
        Task SignOutAsync();
    }
}
