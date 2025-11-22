using System.Threading.Tasks;
using Xunit;
using BloodThinnerTracker.Mobile.ViewModels;
using BloodThinnerTracker.Mobile.Services;

namespace Mobile.UnitTests
{
    public class LoginViewModelTests
    {
        private class FakeAuthSuccess : IAuthService
        {
            public Task<string> SignInAsync() => Task.FromResult("id-token");
            public Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure") => Task.FromResult(true);
            public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(null);
            public Task SignOutAsync() => Task.CompletedTask;
        }

        private class FakeAuthFailExchange : IAuthService
        {
            public Task<string> SignInAsync() => Task.FromResult("id-token");
            public Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure") => Task.FromResult(false);
            public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(null);
            public Task SignOutAsync() => Task.CompletedTask;
        }

        [Fact]
        public async Task SignInAsync_WhenExchangeSucceeds_ReturnsTrue()
        {
            var auth = new FakeAuthSuccess();
            var vm = new LoginViewModel(auth);
            var ok = await vm.SignInAsync();
            Assert.True(ok);
        }

        [Fact]
        public async Task SignInAsync_WhenExchangeFails_ReturnsFalse()
        {
            var auth = new FakeAuthFailExchange();
            var vm = new LoginViewModel(auth);
            var ok = await vm.SignInAsync();
            Assert.False(ok);
        }
    }
}
