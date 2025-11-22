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

        [Fact]
        public void AuthService_Constructor_Succeeds()
        {
            // Act
            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, _mockHttpClient.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(authService);
        }
    }
}

