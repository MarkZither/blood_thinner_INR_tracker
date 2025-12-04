using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using BloodThinnerTracker.Mobile.Services;

namespace Mobile.UnitTests
{
    public class AuthServiceTests
    {
        private readonly Mock<ISecureStorageService> _mockSecureStorage;
        private readonly Mock<IOAuthConfigService> _mockOAuthConfig;
        private readonly ILogger<AuthService> _logger = NullLogger<AuthService>.Instance;

        public AuthServiceTests()
        {
            _mockSecureStorage = new Mock<ISecureStorageService>();
            _mockOAuthConfig = new Mock<IOAuthConfigService>();
        }

        [Fact]
        public async Task GetAccessTokenAsync_ReturnsStoredToken()
        {
            // Arrange
            const string expectedToken = "stored-bearer-token";
            _mockSecureStorage.Setup(x => x.GetAsync("inr_access_token"))
                .ReturnsAsync(expectedToken);

            var features = Options.Create(new FeaturesOptions { ApiRootUrl = "http://api/" });
            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, features, _logger);

            // Act
            var result = await authService.GetAccessTokenAsync();

            // Assert
            Assert.Equal(expectedToken, result);
        }

        [Fact]
        public async Task SignOutAsync_ClearsTokens()
        {
            // Arrange
            var features = Options.Create(new FeaturesOptions { ApiRootUrl = "http://api/" });
            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, features, _logger);

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

            var features = Options.Create(new FeaturesOptions { ApiRootUrl = "http://api/" });
            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, features, _logger);

            // Act & Assert (should not throw)
            await authService.SignOutAsync();
        }

        [Fact]
        public void AuthService_Constructor_Succeeds()
        {
            // Act
            var features = Options.Create(new FeaturesOptions { ApiRootUrl = "http://api/" });
            var authService = new AuthService(_mockSecureStorage.Object, _mockOAuthConfig.Object, features, _logger);

            // Assert
            Assert.NotNull(authService);
        }

        // --- Additional tests for refresh behavior ---

        private class FakeStorageImpl : ISecureStorageService
        {
            private readonly System.Collections.Generic.Dictionary<string, string> _d = new();
            public Task SetAsync(string key, string value) { _d[key] = value; return Task.CompletedTask; }
            public Task<string?> GetAsync(string key) { _d.TryGetValue(key, out var v); return Task.FromResult<string?>(v); }
            public Task RemoveAsync(string key) { _d.Remove(key); return Task.CompletedTask; }
            public Task<(bool success, string? value)> TryGetAsync(string key) { var ok = _d.TryGetValue(key, out var v); return Task.FromResult((ok, ok ? v : null)); }
            public Task<bool> TryRemoveAsync(string key) { var r = _d.Remove(key); return Task.FromResult(r); }
        }

        private static HttpClient CreateClientReturning(string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            var handler = new TestHandler(json, status);
            return new HttpClient(handler) { BaseAddress = new Uri("http://api/") };
        }

        private class TestHandler : HttpMessageHandler
        {
            private readonly string _json;
            private readonly HttpStatusCode _status;
            public TestHandler(string json, HttpStatusCode status) { _json = json; _status = status; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                var resp = new HttpResponseMessage(_status) { Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json") };
                return Task.FromResult(resp);
            }
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_Success_StoresNewTokens()
        {
            var storage = new FakeStorageImpl();
            await storage.SetAsync("inr_refresh_token", "old-refresh");
            var features = Options.Create(new FeaturesOptions { ApiRootUrl = "http://api/" });
            var oauth = new Mock<IOAuthConfigService>();

            var json = JsonSerializer.Serialize(new { accessToken = "new-access", refreshToken = "new-refresh", expiresIn = 3600 });
            using var client = CreateClientReturning(json);

            var svc = new AuthService(storage, oauth.Object, features, NullLogger<AuthService>.Instance, client);

            var ok = await svc.RefreshAccessTokenAsync();
            Assert.True(ok);
            Assert.Equal("new-access", await storage.GetAsync("inr_access_token"));
            Assert.Equal("new-refresh", await storage.GetAsync("inr_refresh_token"));
            Assert.False(string.IsNullOrEmpty(await storage.GetAsync("inr_access_token_expires_at")));
        }

        [Fact]
        public async Task GetAccessTokenAsync_ProactivelyRefreshes_WhenExpiringSoon()
        {
            var storage = new FakeStorageImpl();
            await storage.SetAsync("inr_access_token", "old-access");
            await storage.SetAsync("inr_access_token_expires_at", DateTimeOffset.UtcNow.AddMinutes(4).ToString("o"));
            await storage.SetAsync("inr_refresh_token", "old-refresh");
            var features = Options.Create(new FeaturesOptions { ApiRootUrl = "http://api/" });
            var oauth = new Mock<IOAuthConfigService>();

            var json = JsonSerializer.Serialize(new { accessToken = "rotated-access", refreshToken = "rotated-refresh", expiresIn = 3600 });
            using var client = CreateClientReturning(json);

            var svc = new AuthService(storage, oauth.Object, features, NullLogger<AuthService>.Instance, client);

            var token = await svc.GetAccessTokenAsync();
            Assert.Equal("rotated-access", token);
            Assert.Equal("rotated-access", await storage.GetAsync("inr_access_token"));
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_NoStoredRefreshToken_ReturnsFalse()
        {
            var storage = new FakeStorageImpl();
            // no refresh token stored
            var features = Options.Create(new FeaturesOptions { ApiRootUrl = "http://api/" });
            var oauth = new Mock<IOAuthConfigService>();

            var json = JsonSerializer.Serialize(new { accessToken = "x" });
            using var client = CreateClientReturning(json);

            var svc = new AuthService(storage, oauth.Object, features, NullLogger<AuthService>.Instance, client);

            var ok = await svc.RefreshAccessTokenAsync();
            Assert.False(ok);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_NonSuccessStatus_ReturnsFalseAndDoesNotOverwrite()
        {
            var storage = new FakeStorageImpl();
            await storage.SetAsync("inr_refresh_token", "old-refresh");
            await storage.SetAsync("inr_access_token", "old-access");

            var features = Options.Create(new FeaturesOptions { ApiRootUrl = "http://api/" });
            var oauth = new Mock<IOAuthConfigService>();

            using var client = CreateClientReturning("{\"error\":\"bad\"}", HttpStatusCode.BadRequest);

            var svc = new AuthService(storage, oauth.Object, features, NullLogger<AuthService>.Instance, client);

            var ok = await svc.RefreshAccessTokenAsync();
            Assert.False(ok);
            Assert.Equal("old-access", await storage.GetAsync("inr_access_token"));
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_MalformedJson_ReturnsFalseAndDoesNotOverwrite()
        {
            var storage = new FakeStorageImpl();
            await storage.SetAsync("inr_refresh_token", "old-refresh");
            await storage.SetAsync("inr_access_token", "old-access");

            var features = Options.Create(new FeaturesOptions { ApiRootUrl = "http://api/" });
            var oauth = new Mock<IOAuthConfigService>();

            using var client = CreateClientReturning("not-a-json", HttpStatusCode.OK);

            var svc = new AuthService(storage, oauth.Object, features, NullLogger<AuthService>.Instance, client);

            var ok = await svc.RefreshAccessTokenAsync();
            Assert.False(ok);
            Assert.Equal("old-access", await storage.GetAsync("inr_access_token"));
        }
    }
}

