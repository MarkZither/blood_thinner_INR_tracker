using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Linq;

namespace BloodThinnerTracker.Mobile;

public static class MauiProgram
{
        public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { });

        builder.Services.AddMauiBlazorWebView();

        // Load configuration from appsettings.json embedded resource
        using var stream = typeof(App).Assembly.GetManifestResourceStream("BloodThinnerTracker.Mobile.appsettings.json");
        IConfiguration? preConfig = null;
        if (stream != null)
        {
            preConfig = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddInMemoryCollection(preConfig.AsEnumerable());
        }

        // Serilog initialization is configuration-driven below; keep configuration-driven setup
            // Configure Serilog using configuration (preferred) while ensuring the file path
            // is set to a platform-safe AppDataDirectory. We add a small in-memory override
            // for the Serilog file sink path and ensure a console sink is available as a
            // fallback so something is always logged even if file creation fails.
            try
            {
                // Determine diagnostics options from pre-config (if available)
                var useAppData = preConfig?.GetValue<bool?>("Diagnostics:Serilog:UseAppDataDirectory") ?? true;
                var fileName = preConfig?["Diagnostics:Serilog:FileName"] ?? "mobile-.log";
                var enableConsole = preConfig?.GetValue<bool?>("Diagnostics:Serilog:EnableConsole") ?? true;

                // Enable Serilog SelfLog to Console.Error to capture internal Serilog errors if sinks fail
                Serilog.Debugging.SelfLog.Enable(Console.Error);

                // Build base logger configuration from the merged configuration (embedded appsettings)
                var loggerConfig = new LoggerConfiguration()
                    .ReadFrom.Configuration(builder.Configuration)
                    .Enrich.FromLogContext();

                // If requested, ensure there's a File sink pointing at the platform AppData path.
                if (useAppData)
                {
                    var logsDir = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "Logs");
                    try
                    {
                        System.IO.Directory.CreateDirectory(logsDir);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to create logs directory '{logsDir}': {ex}");
                    }

                    // Use a date-stamped file name to avoid depending on sink-specific enums
                    var dateStamped = fileName.Replace(".log", "");
                    var filePath = System.IO.Path.Combine(logsDir, $"{dateStamped}-{DateTime.UtcNow:yyyy-MM-dd}.log");

                    // Detect if the embedded config already defines a File sink
                    var writeToSection = preConfig?.GetSection("Serilog:WriteTo");
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

                    // If there's no File sink configured, add one programmatically pointing to AppData
                    if (!hasFileSink)
                    {
                        loggerConfig = loggerConfig.WriteTo.File(filePath, shared: true);
                    }

                    // If Console isn't configured and enableConsole is true, add it programmatically
                    if (enableConsole && !hasConsoleSink)
                    {
                        loggerConfig = loggerConfig.WriteTo.Console();
                    }
                }

                // Create the logger and register it
                Log.Logger = loggerConfig.CreateLogger();
                builder.Logging.ClearProviders();
                builder.Logging.AddSerilog(Log.Logger, dispose: true);
            }
            catch (Exception ex)
            {
                // Ensure at least debug logging remains available
                System.Diagnostics.Debug.WriteLine($"Serilog initialization failed: {ex}");
            }

        // Add environment variables (higher priority - overrides appsettings.json)
        // Supports Features__UseMockServices, Features__ApiRootUrl, etc.
        // Read all environment variables and add them to configuration
        var envVars = new Dictionary<string, string?>();
        foreach (System.Collections.DictionaryEntry entry in System.Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString() ?? string.Empty;
            var value = entry.Value?.ToString();
            // Normalize to colon separator for configuration key paths (Features:UseMockServices)
            var normalizedKey = key.Replace("__", ":");
            envVars[normalizedKey] = value;
        }
        builder.Configuration.AddInMemoryCollection(envVars);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        // Register services (DI) - minimal bootstrap
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<AppShell>();
        // Register MainPage for DI resolution in App
        builder.Services.AddSingleton<MainPage>();

        // Register views and viewmodels for navigation
        builder.Services.AddTransient<Views.LoginView>();
        builder.Services.AddTransient<ViewModels.LoginViewModel>();
        builder.Services.AddTransient<Views.InrListView>();
        builder.Services.AddTransient<ViewModels.InrListViewModel>();
        builder.Services.AddTransient<Views.AboutView>();

        // Register Feature services - use configuration flag instead of #if DEBUG
        // Features.UseMockServices: true = mock, false = real API
        // Note: JSON boolean true becomes string "True" (capitalized) - use case-insensitive comparison
        var useMockServices = builder.Configuration["Features:UseMockServices"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        // Bind configuration to strongly-typed FeaturesOptions (no more magic strings!)
        builder.Services.Configure<Services.FeaturesOptions>(builder.Configuration.GetSection(Services.FeaturesOptions.SectionName));

        builder.Services.AddSingleton<Services.EncryptionService>();
        builder.Services.AddSingleton<Services.ISecureStorageService, Services.SecureStorageService>();
        builder.Services.AddSingleton<Services.ICacheService, Services.CacheService>();

        // Register Feature services with mock/real selection
        builder.Services.AddSingleton<Services.IInrService>(sp =>
        {
            return useMockServices
                ? (Services.IInrService)new Services.MockInrService()
                : new Services.ApiInrService(sp.GetRequiredService<System.Net.Http.HttpClient>());
        });

        // HttpClient for API-backed services (uses IOptions to get API root URL)
        builder.Services.AddSingleton<System.Net.Http.HttpClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Services.FeaturesOptions>>();
            return new System.Net.Http.HttpClient
            {
                BaseAddress = new System.Uri(options.Value.ApiRootUrl)
            };
        });
        builder.Services.AddSingleton<Services.ApiInrService>();

        // OAuth configuration service - use configuration flag for mock/real
        builder.Services.AddSingleton<Services.IOAuthConfigService>(sp =>
        {
            return useMockServices
                ? (Services.IOAuthConfigService)new Services.MockOAuthConfigService()
                : new Services.OAuthConfigService(
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Services.FeaturesOptions>>(),
                    sp.GetRequiredService<System.Net.Http.HttpClient>(),
                    sp.GetRequiredService<ILogger<Services.OAuthConfigService>>());
        });

        // Register AuthService - use configuration flag for mock/real
        builder.Services.AddSingleton<Services.IAuthService>(sp =>
        {
            return useMockServices
                ? (Services.IAuthService)new Services.MockAuthService(
                    sp.GetRequiredService<ILogger<Services.MockAuthService>>())
                : new Services.AuthService(
                    sp.GetRequiredService<Services.ISecureStorageService>(),
                    sp.GetRequiredService<Services.IOAuthConfigService>(),
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Services.FeaturesOptions>>(),
                    sp.GetRequiredService<ILogger<Services.AuthService>>());
        });

        return builder.Build();
    }
}
