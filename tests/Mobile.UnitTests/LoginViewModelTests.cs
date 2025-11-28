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
            public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>("access-token-xyz");
            public Task<bool> RefreshAccessTokenAsync()
            {
                return Task.FromResult(true);
            }
            public Task SignOutAsync() => Task.CompletedTask;
        }

        private class FakeAuthFailSignIn : IAuthService
        {
            public Task<string> SignInAsync() => Task.FromResult(string.Empty);
            public Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure") => Task.FromResult(false);
            public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(null);
            public Task<bool> RefreshAccessTokenAsync() => Task.FromResult(false);
            public Task SignOutAsync() => Task.CompletedTask;
        }

        private class FakeAuthFailExchange : IAuthService
        {
            public Task<string> SignInAsync() => Task.FromResult("id-token");
            public Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure") => Task.FromResult(false);
            public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(null);
            public Task<bool> RefreshAccessTokenAsync() => Task.FromResult(false);
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
            Assert.Equal("azure", vm.SelectedProvider);
        }

        [Fact]
        public async Task LoginViewModel_SignInWithAzureAsync_SucceedsWithValidAuth()
        {
            // Arrange
            var auth = new FakeAuthSuccess();
            var vm = new LoginViewModel(auth);
            bool loginSucceededCalled = false;
            vm.LoginSucceeded += (s, e) => loginSucceededCalled = true;

            // Act - Call the method directly
            await vm.SignInWithAzureAsync();

            // Assert
            Assert.True(loginSucceededCalled);
            Assert.Null(vm.ErrorMessage);
            Assert.False(vm.IsBusy);
            Assert.Equal("azure", vm.SelectedProvider);
        }

        [Fact]
        public async Task LoginViewModel_SignInWithGoogleAsync_SucceedsWithValidAuth()
        {
            // Arrange
            var auth = new FakeAuthSuccess();
            var vm = new LoginViewModel(auth);
            bool loginSucceededCalled = false;
            vm.LoginSucceeded += (s, e) => loginSucceededCalled = true;

            // Act
            await vm.SignInWithGoogleAsync();

            // Assert
            Assert.True(loginSucceededCalled);
            Assert.Null(vm.ErrorMessage);
            Assert.False(vm.IsBusy);
            Assert.Equal("google", vm.SelectedProvider);
        }

        [Fact]
        public async Task LoginViewModel_SignInWithAzureAsync_SetsErrorOnFailedSignIn()
        {
            // Arrange
            var auth = new FakeAuthFailSignIn();
            var vm = new LoginViewModel(auth);
            bool loginSucceededCalled = false;
            vm.LoginSucceeded += (s, e) => loginSucceededCalled = true;

            // Act
            await vm.SignInWithAzureAsync();

            // Assert
            Assert.False(loginSucceededCalled);
            Assert.NotNull(vm.ErrorMessage);
            Assert.Contains("failed", vm.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public async Task LoginViewModel_SignInWithGoogleAsync_SetsErrorOnFailedExchange()
        {
            // Arrange
            var auth = new FakeAuthFailExchange();
            var vm = new LoginViewModel(auth);
            bool loginSucceededCalled = false;
            vm.LoginSucceeded += (s, e) => loginSucceededCalled = true;

            // Act
            await vm.SignInWithGoogleAsync();

            // Assert
            Assert.False(loginSucceededCalled);
            Assert.NotNull(vm.ErrorMessage);
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public void LoginViewModel_PropertyChanged_FiresForIsBusy()
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


