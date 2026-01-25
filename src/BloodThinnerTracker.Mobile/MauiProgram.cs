using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BloodThinnerTracker.Mobile.Extensions;

namespace BloodThinnerTracker.Mobile;

/// <summary>
/// MAUI application entry point and DI container configuration.
/// Service registration is delegated to extension methods for maintainability.
/// </summary>
public static partial class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { });

        builder.Services.AddMauiBlazorWebView();

        // ═══════════════════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════════════
        ConfigureAppSettings(builder);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // ═══════════════════════════════════════════════════════════════════════════
        // CORE SERVICES (used by both mock and real modes)
        // ═══════════════════════════════════════════════════════════════════════════
        builder.Services.AddCoreServices();
        builder.Services.AddViewsAndViewModels();
        builder.Services.AddApiHttpClient();
        builder.Services.AddBackgroundServices(builder.Configuration);

        // ═══════════════════════════════════════════════════════════════════════════
        // MOCK vs REAL SERVICE REGISTRATION - Single consolidated block
        // ═══════════════════════════════════════════════════════════════════════════
        var useMockServices = builder.Configuration["Features:UseMockServices"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        var skipPlatformServices = builder.Configuration["Test:SkipPlatformServices"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        if (useMockServices)
        {
            // Mock mode: all services use in-memory data, no remote API calls
            builder.Services.AddMockServices();
        }
        else
        {
            // Real mode: services call the remote API
            builder.Services.AddRealServices(builder.Configuration, skipPlatformServices);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // PLATFORM-SPECIFIC SERVICES
        // ═══════════════════════════════════════════════════════════════════════════
        RegisterPlatformServices(builder);

        // ═══════════════════════════════════════════════════════════════════════════
        // BUILD AND INITIALIZE
        // ═══════════════════════════════════════════════════════════════════════════
        var app = builder.Build();

        return app
            .RunStartupDiagnostics(builder.Configuration)
            .ApplyDatabaseMigrations();
    }

    /// <summary>
    /// Configures application settings from embedded resources and environment variables.
    /// </summary>
    private static void ConfigureAppSettings(MauiAppBuilder builder)
    {
        // Load configuration from appsettings.json embedded resource
        using var stream = typeof(App).Assembly.GetManifestResourceStream("BloodThinnerTracker.Mobile.appsettings.json");
        if (stream != null)
        {
            var preConfig = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddInMemoryCollection(preConfig.AsEnumerable());
        }

        // Configure Serilog logging via extension
        builder.AddLogging(builder.Configuration);

        // Add environment variables (higher priority - overrides appsettings.json)
        var envVars = new Dictionary<string, string?>();
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString() ?? string.Empty;
            var value = entry.Value?.ToString();
            // Normalize to colon separator for configuration key paths (Features:UseMockServices)
            var normalizedKey = key.Replace("__", ":");
            envVars[normalizedKey] = value;
        }
        builder.Configuration.AddInMemoryCollection(envVars);

        // Register options
        builder.Services.AddSplashOptions(builder.Configuration);
        builder.Services.AddFeaturesOptions(builder.Configuration);
    }

    // Partial method implemented in platform-specific files (no-op in shared code)
    static partial void RegisterPlatformServices(MauiAppBuilder builder);
}
