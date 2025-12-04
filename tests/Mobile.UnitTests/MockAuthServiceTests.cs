using System;
using System.Threading.Tasks;
using Xunit;
using BloodThinnerTracker.Mobile.Services;

namespace Mobile.UnitTests
{
    public class MockAuthServiceTests
    {
        private readonly MockLogger<MockAuthService> _mockLogger = new();

        [Fact]
        public void MockAuthService_Constructor_Succeeds()
        {
            // Act
            var service = new MockAuthService(_mockLogger);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task SignInAsync_ReturnsMockIdToken()
        {
            // Arrange
            var service = new MockAuthService(_mockLogger);

            // Act
            var idToken = await service.SignInAsync();

            // Assert
            Assert.NotEmpty(idToken);
            Assert.Contains("mock", idToken.ToLower());
        }

        [Fact]
        public async Task ExchangeIdTokenAsync_WithValidIdToken_ReturnsTrue()
        {
            // Arrange
            var service = new MockAuthService(_mockLogger);
            var idToken = "mock-id-token-123";

            // Act
            var result = await service.ExchangeIdTokenAsync(idToken, "azure");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExchangeIdTokenAsync_WithEmptyIdToken_ReturnsFalse()
        {
            // Arrange
            var service = new MockAuthService(_mockLogger);

            // Act
            var result = await service.ExchangeIdTokenAsync(string.Empty, "azure");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetAccessTokenAsync_AfterExchange_ReturnsBearerToken()
        {
            // Arrange
            var service = new MockAuthService(_mockLogger);
            await service.ExchangeIdTokenAsync("mock-id-token-123", "azure");

            // Act
            var token = await service.GetAccessTokenAsync();

            // Assert
            Assert.NotNull(token);
            Assert.Contains("mock-bearer-token", token);
        }

        [Fact]
        public async Task GetAccessTokenAsync_BeforeExchange_ReturnsNull()
        {
            // Arrange
            var service = new MockAuthService(_mockLogger);

            // Act
            var token = await service.GetAccessTokenAsync();

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task SignOutAsync_ClearsAccessToken()
        {
            // Arrange
            var service = new MockAuthService(_mockLogger);
            await service.ExchangeIdTokenAsync("mock-id-token-123", "azure");
            var tokenBefore = await service.GetAccessTokenAsync();
            Assert.NotNull(tokenBefore);

            // Act
            await service.SignOutAsync();

            // Assert
            var token = await service.GetAccessTokenAsync();
            Assert.Null(token);
        }

        [Fact]
        public async Task ExchangeIdTokenAsync_SupportsMultipleProviders()
        {
            // Arrange
            var service = new MockAuthService(_mockLogger);

            // Act - Azure
            var resultAzure = await service.ExchangeIdTokenAsync("mock-id-token-123", "azure");
            Assert.True(resultAzure);

            // Act - Google
            var resultGoogle = await service.ExchangeIdTokenAsync("mock-id-token-456", "google");
            Assert.True(resultGoogle);

            // Assert
            var token = await service.GetAccessTokenAsync();
            Assert.NotNull(token);
        }
    }
}
