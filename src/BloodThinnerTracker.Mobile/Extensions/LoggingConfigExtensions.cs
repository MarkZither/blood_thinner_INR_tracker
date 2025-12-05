using System;
using System.IO;
using Microsoft.Maui;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BloodThinnerTracker.Mobile.Extensions
{
    public static class LoggingConfigExtensions
    {
        /// <summary>
        /// Configure Serilog logging for the MAUI app using the provided configuration.
        /// Mirrors previous inline setup in `MauiProgram.CreateMauiApp()` and preserves
        /// file-sink fallback to AppData directory and console sink fallback.
        /// </summary>
        public static MauiAppBuilder AddLogging(this MauiAppBuilder builder, IConfiguration configuration)
        {
            try
            {
                var useAppData = configuration.GetValue<bool?>("Diagnostics:Serilog:UseAppDataDirectory") ?? true;
                var fileName = configuration["Diagnostics:Serilog:FileName"] ?? "mobile-.log";
                var enableConsole = configuration.GetValue<bool?>("Diagnostics:Serilog:EnableConsole") ?? true;

                // Ensure Serilog internal SelfLog writes to Console.Error for diagnostics
                Serilog.Debugging.SelfLog.Enable(Console.Error);

                var loggerConfig = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .Enrich.FromLogContext();

                if (useAppData)
                {
                    var logsDir = Path.Combine(FileSystem.AppDataDirectory, "Logs");
                    try
                    {
                        Directory.CreateDirectory(logsDir);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Debugging.SelfLog.WriteLine($"Failed to create logs directory '{logsDir}': {ex}");
                    }

                    var dateStamped = fileName.Replace(".log", "");
                    var filePath = Path.Combine(logsDir, $"{dateStamped}-{DateTime.UtcNow:yyyy-MM-dd}.log");

                    var writeToSection = configuration.GetSection("Serilog:WriteTo");
                    var hasFileSink = false;
                    var hasConsoleSink = false;
                    if (writeToSection != null)
                    {
                        foreach (var child in writeToSection.GetChildren())
                        {
                            var name = child["Name"];
                            if (string.Equals(name, "File", StringComparison.OrdinalIgnoreCase)) hasFileSink = true;
                            if (string.Equals(name, "Console", StringComparison.OrdinalIgnoreCase)) hasConsoleSink = true;
                        }
                    }

                    if (!hasFileSink)
                    {
                        loggerConfig = loggerConfig.WriteTo.File(filePath, shared: true);
                    }

                    if (enableConsole && !hasConsoleSink)
                    {
                        loggerConfig = loggerConfig.WriteTo.Console();
                    }
                }

                Log.Logger = loggerConfig.CreateLogger();
                builder.Logging.ClearProviders();
                builder.Logging.AddSerilog(Log.Logger, dispose: true);
            }
            catch (Exception ex)
            {
                Serilog.Debugging.SelfLog.WriteLine($"Serilog initialization failed: {ex}");
            }

            return builder;
        }
    }
}
