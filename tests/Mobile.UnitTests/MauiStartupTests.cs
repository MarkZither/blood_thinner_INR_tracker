using System;
using Xunit;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.UnitTests
{
    public class MauiStartupTests
    {
        [Fact]
        public void CreateMauiApp_DoesNotThrowAndResolvesCoreServices()
        {
            // Temporarily set environment variables, but restore previous values to avoid
            // contaminating other tests that rely on environment configuration.
            var prevSkip = Environment.GetEnvironmentVariable("Test__SkipPlatformServices");
            var prevUseMock = Environment.GetEnvironmentVariable("Features__UseMockServices");
            var prevApiRoot = Environment.GetEnvironmentVariable("Features__ApiRootUrl");

            try
            {
                Environment.SetEnvironmentVariable("Test__SkipPlatformServices", "true");
                Environment.SetEnvironmentVariable("Features__UseMockServices", "true");
                Environment.SetEnvironmentVariable("Features__ApiRootUrl", "https://localhost/");

                var app = BloodThinnerTracker.Mobile.MauiProgram.CreateMauiApp();
                Assert.NotNull(app);

                using var scope = app.Services.CreateScope();
                var provider = scope.ServiceProvider;

                // Core services that previously caused runtime DI failures
                var auth = provider.GetService<IAuthService>();
                var cache = provider.GetService<ICacheService>();
                var secure = provider.GetService<ISecureStorageService>();

                Assert.NotNull(auth);
                Assert.NotNull(cache);
                Assert.NotNull(secure);
            }
            finally
            {
                Environment.SetEnvironmentVariable("Test__SkipPlatformServices", prevSkip);
                Environment.SetEnvironmentVariable("Features__UseMockServices", prevUseMock);
                Environment.SetEnvironmentVariable("Features__ApiRootUrl", prevApiRoot);
            }
        }
    }
}
