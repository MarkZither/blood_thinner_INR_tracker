using System.Threading.Tasks;
using Xunit;
using BloodThinnerTracker.Mobile.ViewModels;
using BloodThinnerTracker.Mobile.Services;

namespace Mobile.UnitTests
{
    public class SplashViewModelTests
    {
        private class FakeAuth : IAuthService
        {
            private readonly string? _token;
            public FakeAuth(string? token) { _token = token; }
            public Task<string> SignInAsync() => Task.FromResult("fake-id");
            public Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure") => Task.FromResult(true);
            public Task<string?> GetAccessTokenAsync() => Task.FromResult(_token);
            public Task SignOutAsync() { return Task.CompletedTask; }
        }

        [Fact]
        public async Task InitializeAsync_WithToken_SetsIsAuthenticatedTrue()
        {
            var auth = new FakeAuth("token123");
            var vm = new SplashViewModel(auth);
            await vm.InitializeAsync();
            Assert.True(vm.IsAuthenticated);
        }

        [Fact]
        public async Task InitializeAsync_WithoutToken_SetsIsAuthenticatedFalse()
        {
            var auth = new FakeAuth(null);
            var vm = new SplashViewModel(auth);
            await vm.InitializeAsync();
            Assert.False(vm.IsAuthenticated);
        }
    }
}
