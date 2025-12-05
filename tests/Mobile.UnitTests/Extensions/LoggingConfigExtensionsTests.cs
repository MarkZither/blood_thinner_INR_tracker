using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Extensions.Configuration;
using Serilog;
using Xunit;
using BloodThinnerTracker.Mobile.Extensions;

namespace Mobile.UnitTests.Extensions
{
    public class LoggingConfigExtensionsTests
    {
        [Fact]
        public void AddLogging_Configures_SerilogLogger()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    { "Diagnostics:Serilog:UseAppDataDirectory", "false" },
                    { "Diagnostics:Serilog:EnableConsole", "false" }
                })
                .Build();

            var builder = MauiApp.CreateBuilder();
            builder.AddLogging(config);

            Assert.NotNull(Log.Logger);
            Assert.IsType<Serilog.Core.Logger>(Log.Logger);

            // Clean up logger resources
            Log.CloseAndFlush();
        }
    }
}
