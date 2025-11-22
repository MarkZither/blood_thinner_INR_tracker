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
        public async Task SignInAsyncCommand_WhenSignInSucceeds_NoErrorMessage()
        {
            // Arrange
            var auth = new FakeAuthSuccess();
            var vm = new LoginViewModel(auth);

            // Act
            await vm.SignInAsyncCommand.ExecuteAsync(null);

            // Assert
            Assert.Null(vm.ErrorMessage);
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public async Task SignInAsyncCommand_WhenSignInFails_SetsErrorMessage()
        {
            // Arrange
            var auth = new FakeAuthFailSignIn();
            var vm = new LoginViewModel(auth);

            // Act
            await vm.SignInAsyncCommand.ExecuteAsync(null);

            // Assert
            Assert.NotNull(vm.ErrorMessage);
            Assert.Contains("failed", vm.ErrorMessage.ToLowerInvariant());
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public async Task SignInAsyncCommand_WhenExchangeFails_SetsErrorMessage()
        {
            // Arrange
            var auth = new FakeAuthFailExchange();
            var vm = new LoginViewModel(auth);

            // Act
            await vm.SignInAsyncCommand.ExecuteAsync(null);

            // Assert
            Assert.NotNull(vm.ErrorMessage);
            Assert.Contains("Unable to complete", vm.ErrorMessage);
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public async Task SignInAsyncCommand_SetsBusyDuringExecution()
        {
            // Arrange
            var auth = new FakeAuthSuccess();
            var vm = new LoginViewModel(auth);
            bool busyDuringExecution = false;

            // Track IsBusy state during execution
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LoginViewModel.IsBusy) && vm.IsBusy)
                {
                    busyDuringExecution = true;
                }
            };

            // Act
            await vm.SignInAsyncCommand.ExecuteAsync(null);

            // Assert
            Assert.True(busyDuringExecution);
            Assert.False(vm.IsBusy); // Should be false after completion
        }
    }
}

