using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Shared.Models.Authentication;

namespace Mobile.UnitTests
{
    public class OAuthConfigServiceTests
    {
        private class StubCacheService : ICacheService
        {
            public Task SetAsync(string key, string jsonPayload, TimeSpan? expiresIn = null) => Task.CompletedTask;
            public Task<string?> GetAsync(string key) => Task.FromResult<string?>(null);
            public Task<bool> HasValidCacheAsync(string key) => Task.FromResult(false);
            public Task<long?> GetCacheAgeMillisecondsAsync(string key) => Task.FromResult<long?>(null);
            public Task ClearAsync(string key) => Task.CompletedTask;
            public Task<DateTime?> GetExpirationTimeAsync(string key) => Task.FromResult<DateTime?>(null);
        }

        [Fact]
        public void OAuthConfigService_Constructor_Succeeds()
        {
            // Arrange
            var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost") };
            var mockLogger = new MockLogger<OAuthConfigService>();
            var features = Microsoft.Extensions.Options.Options.Create(new FeaturesOptions { ApiRootUrl = "http://localhost" });
            var cacheService = new StubCacheService();

            // Act
            var service = new OAuthConfigService(features, httpClient, mockLogger, cacheService);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task GetConfigAsync_WithHttpException_ReturnsNull()
        {
            // Arrange
            var httpClient = new HttpClient();
            var mockLogger = new MockLogger<OAuthConfigService>();
            var features = Microsoft.Extensions.Options.Options.Create(new FeaturesOptions { ApiRootUrl = "http://localhost" });
            var cacheService = new StubCacheService();
            var service = new OAuthConfigService(features, httpClient, mockLogger, cacheService);

            // Act
            var result = await service.GetConfigAsync();

            // Assert
            // Service should return null when API is unavailable
            Assert.Null(result);
        }

        [Fact]
        public async Task MockOAuthConfigService_ReturnsValidConfig()
        {
            // Arrange
            var service = new MockOAuthConfigService();

            // Act
            var result = await service.GetConfigAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Providers ?? new List<OAuthProviderConfig>());
            Assert.True(result.Providers!.Any(p => p.Provider == "azure"));
            Assert.True(result.Providers!.Any(p => p.Provider == "google"));
        }
    }

    /// <summary>
    /// Minimal mock logger for testing.
    /// </summary>
    internal class MockLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) { }
    }
}

