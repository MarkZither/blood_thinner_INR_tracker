using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using BloodThinnerTracker.Web.Services;
using BloodThinnerTracker.Shared.Models.Authentication;

namespace BloodThinnerTracker.Web.Tests.Services
{
    // Helper class for session feature
    public class DefaultSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }

    public class TokenRefreshTests
    {
        private DefaultHttpContext CreateHttpContextWithSession(string sessionId = "test-session-123")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "test-user-123")
            }, "test"));
            
            // Mock session
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.Id).Returns(sessionId);
            mockSession.Setup(s => s.IsAvailable).Returns(true);
            httpContext.Features.Set<ISessionFeature>(new DefaultSessionFeature { Session = mockSession.Object });
            
            return httpContext;
        }

        private Mock<IMemoryCache> CreateMockCache()
        {
            var mockCache = new Mock<IMemoryCache>();
            var cacheDictionary = new System.Collections.Concurrent.ConcurrentDictionary<object, object>();

            mockCache
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns((object key) =>
                {
                    var mockEntry = new Mock<ICacheEntry>();
                    mockEntry.SetupGet(e => e.Key).Returns(key);
                    mockEntry.SetupProperty(e => e.Value);
                    mockEntry.SetupProperty(e => e.AbsoluteExpiration);
                    mockEntry.SetupProperty(e => e.SlidingExpiration);
                    mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                    mockEntry.Setup(e => e.Dispose()).Callback(() =>
                    {
                        cacheDictionary[key] = mockEntry.Object.Value!;
                    });
                    return mockEntry.Object;
                });

            mockCache
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny!))
                .Returns((object key, out object? value) =>
                {
                    var found = cacheDictionary.TryGetValue(key, out var objValue);
                    value = objValue;
                    return found;
                });

            return mockCache;
        }

        private string CreateJwtToken(DateTimeOffset expiry)
        {
            // Create a simple JWT-like structure for testing
            var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
            var payload = JsonSerializer.Serialize(new
            {
                sub = "test-user-123",
                email = "test@example.com",
                name = "Test User",
                exp = expiry.ToUnixTimeSeconds()
            });
            var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-signature"));

            return $"{header}.{payloadBase64}.{signature}";
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_ExpiredToken_TriggersRefresh()
        {
            // Arrange
            var mockCache = CreateMockCache();
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockLogger = new Mock<ILogger<CustomAuthenticationStateProvider>>();
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["ApiBaseUrl"]).Returns("https://localhost:7234");
            
            // Mock HttpClient that returns successful refresh response
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var expiredToken = CreateJwtToken(DateTimeOffset.UtcNow.AddMinutes(-10)); // Expired 10 minutes ago
            var newToken = CreateJwtToken(DateTimeOffset.UtcNow.AddHours(1)); // Valid for 1 hour
            
            var refreshResponse = new AuthenticationResponse
            {
                AccessToken = newToken,
                RefreshToken = "new-refresh-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                User = new UserInfo
                {
                    Email = "test@example.com",
                    Name = "Test User",
                    PublicId = Guid.NewGuid()
                }
            };

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/auth/refresh")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(refreshResponse)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://localhost:7234")
            };

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var httpContext = CreateHttpContextWithSession();
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var provider = new CustomAuthenticationStateProvider(
                mockCache.Object,
                mockHttpContextAccessor.Object,
                mockLogger.Object,
                mockHttpClientFactory.Object,
                mockConfiguration.Object);

            // Store expired token and refresh token
            await provider.MarkUserAsAuthenticatedAsync(
                expiredToken,
                "old-refresh-token",
                httpContext.User);

            // Act
            var authState = await provider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(authState);
            // After expired token, refresh should be attempted
            // We can't easily verify the new token was stored due to private methods,
            // but we can verify the HTTP call was made
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/auth/refresh")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_TokenExpiresInFiveMinutes_ProactiveRefresh()
        {
            // Arrange
            var mockCache = CreateMockCache();
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockLogger = new Mock<ILogger<CustomAuthenticationStateProvider>>();
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["ApiBaseUrl"]).Returns("https://localhost:7234");
            
            // Token expires in 4 minutes (should trigger proactive refresh)
            var soonToExpireToken = CreateJwtToken(DateTimeOffset.UtcNow.AddMinutes(4));
            var newToken = CreateJwtToken(DateTimeOffset.UtcNow.AddHours(1));
            
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var refreshResponse = new AuthenticationResponse
            {
                AccessToken = newToken,
                RefreshToken = "new-refresh-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                User = new UserInfo
                {
                    Email = "test@example.com",
                    Name = "Test User",
                    PublicId = Guid.NewGuid()
                }
            };

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/auth/refresh")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(refreshResponse)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://localhost:7234")
            };

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var httpContext = CreateHttpContextWithSession("test-session-456");
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var provider = new CustomAuthenticationStateProvider(
                mockCache.Object,
                mockHttpContextAccessor.Object,
                mockLogger.Object,
                mockHttpClientFactory.Object,
                mockConfiguration.Object);

            await provider.MarkUserAsAuthenticatedAsync(
                soonToExpireToken,
                "old-refresh-token",
                httpContext.User);

            // Act
            var authState = await provider.GetAuthenticationStateAsync();

            // Assert - should return current token but trigger background refresh
            Assert.NotNull(authState);
            Assert.True(authState.User.Identity?.IsAuthenticated);
            
            // Wait a bit for background task to start
            await Task.Delay(100);
            
            // Verify refresh was called (eventually)
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/auth/refresh")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_ValidToken_NoRefresh()
        {
            // Arrange
            var mockCache = CreateMockCache();
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockLogger = new Mock<ILogger<CustomAuthenticationStateProvider>>();
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["ApiBaseUrl"]).Returns("https://localhost:7234");
            
            // Token valid for 1 hour (should not trigger refresh)
            var validToken = CreateJwtToken(DateTimeOffset.UtcNow.AddHours(1));
            
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://localhost:7234")
            };

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var httpContext = CreateHttpContextWithSession("test-session-789");
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var provider = new CustomAuthenticationStateProvider(
                mockCache.Object,
                mockHttpContextAccessor.Object,
                mockLogger.Object,
                mockHttpClientFactory.Object,
                mockConfiguration.Object);

            await provider.MarkUserAsAuthenticatedAsync(
                validToken,
                "refresh-token",
                httpContext.User);

            // Act
            var authState = await provider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(authState);
            Assert.True(authState.User.Identity?.IsAuthenticated);
            
            // Verify refresh was NOT called
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/auth/refresh")),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
