using System;
using Microsoft.Maui.Hosting;
using Xunit;

namespace BloodThinnerTracker.Mobile.UnitTests
{
    [Collection("Maui")]
    public class MauiProgramConfigTests
    {
        [Theory]
        [InlineData("true", "MockInrService")]
        [InlineData("false", "ApiInrService")]
        public void UseMockServices_ConfiguresProperIInrService(string envValue, string expectedTypeName)
        {
            var envKey = "Features__UseMockServices";
            var orig = Environment.GetEnvironmentVariable(envKey);
            try
            {
                Environment.SetEnvironmentVariable(envKey, envValue);

                var app = MauiProgram.CreateMauiApp();
                var svc = app.Services.GetService(typeof(BloodThinnerTracker.Mobile.Services.IInrService));

                Assert.NotNull(svc);
                var actualTypeName = svc.GetType().Name;
                Assert.Equal(expectedTypeName, actualTypeName);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envKey, orig);
            }
        }

        [Fact]
        public void CacheSyncService_IsSingletonAndMappedToIHostedService()
        {
            var envKey = "Test__SkipPlatformServices";
            var orig = Environment.GetEnvironmentVariable(envKey);
            try
            {
                Environment.SetEnvironmentVariable(envKey, "true");

                var app = MauiProgram.CreateMauiApp();

                var cacheSync = app.Services.GetRequiredService<BloodThinnerTracker.Mobile.Services.CacheSyncService>();
                var hosted = app.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostedService>();

                Assert.Same(cacheSync, hosted);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envKey, orig);
            }
        }
    }
}
