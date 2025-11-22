using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Shared.Models.Authentication;

namespace Mobile.UnitTests
{
    public class AuthServiceTests
    {
        private readonly Mock<ISecureStorageService> _mockSecureStorage;
        private readonly Mock<IOAuthConfigService> _mockOAuthConfig;
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<ILogger<AuthService>> _mockLogger;

        public AuthServiceTests()
        {
            _mockSecureStorage = new Mock<ISecureStorageService>();
            _mockOAuthConfig = new Mock<IOAuthConfigService>();
            _mockHttpClient = new Mock<HttpClient>();
            _mockLogger = new Mock<ILogger<AuthService>>();
        }

        [Fact]
        public async Task SignInAsync_WithNullConfig_ReturnsEmpty()
        {
            // Arrange
            _mockOAuthConfig.Setup(x => x.GetConfigAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((OAuthConfig?)null);

            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, _mockHttpClient.Object, _mockLogger.Object);

            // Act
            var result = await authService.SignInAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SignInAsync_WithEmptyProviderList_ReturnsEmpty()
        {
            // Arrange
            var config = new OAuthConfig { Providers = new List<OAuthProviderConfig>() };
            _mockOAuthConfig.Setup(x => x.GetConfigAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);

            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, _mockHttpClient.Object, _mockLogger.Object);

            // Act
            var result = await authService.SignInAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SignInAsync_WithMissingAzureProvider_ReturnsEmpty()
        {
            // Arrange
            var config = new OAuthConfig
            {
                Providers = new List<OAuthProviderConfig>
                {
                    new OAuthProviderConfig { Provider = "google", ClientId = "123", Authority = "https://google.com", RedirectUri = "http://localhost" }
                }
            };
            _mockOAuthConfig.Setup(x => x.GetConfigAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);

            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, _mockHttpClient.Object, _mockLogger.Object);

            // Act
            var result = await authService.SignInAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ExchangeIdTokenAsync_WithNullIdToken_ReturnsFalse()
        {
            // Arrange
            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, _mockHttpClient.Object, _mockLogger.Object);

            // Act
            var result = await authService.ExchangeIdTokenAsync(string.Empty);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExchangeIdTokenAsync_WithFailedExchange_ReturnsFalse()
        {
            // Arrange
            var mockResponse = new Mock<HttpResponseMessage>();
            mockResponse.Setup(x => x.IsSuccessStatusCode).Returns(false);
            mockResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.Unauthorized);

            _mockHttpClient.Setup(x => x.PostAsJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, _mockHttpClient.Object, _mockLogger.Object);

            // Act
            var result = await authService.ExchangeIdTokenAsync("valid-id-token");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetAccessTokenAsync_ReturnsStoredToken()
        {
            // Arrange
            const string expectedToken = "stored-bearer-token";
            _mockSecureStorage.Setup(x => x.GetAsync("inr_access_token"))
                .ReturnsAsync(expectedToken);

            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, _mockHttpClient.Object, _mockLogger.Object);

            // Act
            var result = await authService.GetAccessTokenAsync();

            // Assert
            Assert.Equal(expectedToken, result);
        }

        [Fact]
        public async Task SignOutAsync_ClearsTokens()
        {
            // Arrange
            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, _mockHttpClient.Object, _mockLogger.Object);

            // Act
            await authService.SignOutAsync();

            // Assert
            _mockSecureStorage.Verify(x => x.RemoveAsync("inr_access_token"), Times.Once);
            _mockSecureStorage.Verify(x => x.RemoveAsync("inr_id_token"), Times.Once);
        }

        [Fact]
        public async Task SignOutAsync_WithException_DoesNotThrow()
        {
            // Arrange
            _mockSecureStorage.Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Storage failure"));

            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, _mockHttpClient.Object, _mockLogger.Object);

            // Act & Assert (should not throw)
            await authService.SignOutAsync();
        }
    }
}
