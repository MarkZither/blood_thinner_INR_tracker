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

        private class FakeAuthFailSignIn : IAuthService
        {
            public Task<string> SignInAsync() => Task.FromResult(string.Empty);
            public Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure") => Task.FromResult(false);
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
        public void LoginViewModel_Constructor_SucceedsWithAuthService()
        {
            // Arrange
            var auth = new FakeAuthSuccess();

            // Act
            var vm = new LoginViewModel(auth);

            // Assert
            Assert.NotNull(vm);
            Assert.Null(vm.ErrorMessage);
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public async Task LoginViewModel_InitiallyHasNoErrorMessage()
        {
            // Arrange
            var auth = new FakeAuthSuccess();
            var vm = new LoginViewModel(auth);

            // Assert
            Assert.Null(vm.ErrorMessage);
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public void LoginViewModel_PropertyChangedFires()
        {
            // Arrange
            var auth = new FakeAuthSuccess();
            var vm = new LoginViewModel(auth);
            int propertyChangedCount = 0;

            vm.PropertyChanged += (s, e) => propertyChangedCount++;

            // Act
            vm.IsBusy = true;

            // Assert
            Assert.True(vm.IsBusy);
            Assert.True(propertyChangedCount > 0);
        }
    }
}


