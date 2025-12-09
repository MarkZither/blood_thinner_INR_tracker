using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using BloodThinnerTracker.Mobile.Extensions;
using BloodThinnerTracker.Mobile.Config;
using System.Linq;

namespace BloodThinnerTracker.Mobile;

public static partial class MauiProgram
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

        // Configure Serilog logging via extension to keep MauiProgram concise
        builder.AddLogging(builder.Configuration);

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

        // Register SplashOptions and its validation via extension to keep startup concise
        builder.Services.AddSplashOptions(builder.Configuration);
        // ensure the extension namespace is available

        // Register optional app initializer (warms tokens, remote config fetch, etc.)
        builder.Services.AddSingleton<Services.IAppInitializer, Services.AppInitializer>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        // Register services (DI) - minimal bootstrap
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<AppShell>();
        // Theme service used by AppShell for persisted theme preference
        builder.Services.AddSingleton<Services.IThemeService, Services.ThemeService>();
        // Register MainPage for DI resolution in App
        builder.Services.AddSingleton<MainPage>();

        // Register views and viewmodels for navigation
        builder.Services.AddTransient<Views.LoginView>();
        builder.Services.AddTransient<ViewModels.LoginViewModel>();
        builder.Services.AddTransient<Views.InrListView>();
        builder.Services.AddTransient<ViewModels.InrListViewModel>();
        // Lazy factory registration for deferred ViewModel creation
        builder.Services.AddTransient(typeof(BloodThinnerTracker.Mobile.Extensions.LazyViewModelFactory<>));
        builder.Services.AddTransient<Views.AboutView>();

        // Register Feature services - use configuration flag instead of #if DEBUG
        // Features.UseMockServices: true = mock, false = real API
        // Note: JSON boolean true becomes string "True" (capitalized) - use case-insensitive comparison
        var useMockServices = builder.Configuration["Features:UseMockServices"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        builder.Services.AddSingleton<Services.IInrService>(sp =>
        {
            return useMockServices
                ? (Services.IInrService)new Services.MockInrService()
                : sp.GetRequiredService<Services.ApiInrService>();
        });

        // Register medication mock/in-memory implementations when feature flag enabled
            if (useMockServices)
            {
                builder.Services.AddSingleton<Services.IMedicationService, Services.MockMedicationService>();
            builder.Services.AddSingleton<Services.IMedicationRepository, Services.InMemoryMedicationRepository>();
            builder.Services.AddSingleton<Services.IMedicationLogRepository, Services.InMemoryMedicationLogRepository>();
        }

        // If the Data.SQLite project is available, register a read-only adapter to query
        // the canonical local DB for offline reads. This keeps the mobile app consistent
        // with server-side schema while remaining read-only.
        // Allow tests to opt-out of platform-specific registration by setting
        // configuration `Test:SkipPlatformServices=true` (in-memory or environment).
        var skipPlatformServices = builder.Configuration["Test:SkipPlatformServices"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        if (!skipPlatformServices)
            {
                try
                {
            // Register Data Protection services required by ApplicationDbContext for encryption
            var dataProtectionPath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "DataProtection-Keys");
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new System.IO.DirectoryInfo(dataProtectionPath))
                .SetApplicationName("BloodThinnerTracker.Mobile");

            // Register mobile-specific ICurrentUserService (single-user device)
            // Also register a seeder that ensures the device-local user exists
            builder.Services.AddScoped<Services.MobileUserSeeder>();
            builder.Services.AddScoped<BloodThinnerTracker.Data.Shared.ICurrentUserService, Services.MobileCurrentUserService>();

            builder.Services.AddScoped<Services.IInrRepository, Services.InrRepository>();
                // Register medication repositories only when not using mock services
                if (!useMockServices)
                {
                    builder.Services.AddScoped<Services.IMedicationRepository, Services.MedicationRepository>();
                    builder.Services.AddScoped<Services.IMedicationLogRepository, Services.MedicationLogRepository>();
                }
            // Register the EF DbContext from Data.SQLite using a file-based connection.
            var dbPath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "bloodtracker_mobile.db");
            builder.Services.AddDbContext<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>(opts =>
            {
                opts.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("BloodThinnerTracker.Data.SQLite"));
            });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MauiProgram: SQLite/DataProtection registration failed: {ex.Message}");
                }
            }
            else
            {
                // Test-mode fallbacks: register lightweight in-memory implementations so
                // critical background services can be resolved without platform DB.
                builder.Services.AddSingleton<Services.IInrService, Services.MockInrService>();
                builder.Services.AddSingleton<Services.IInrRepository, Services.InMemoryInrRepository>();
                // Also ensure medication mocks are available in test-mode
                if (!useMockServices)
                {
                    // If test-mode but feature flag says real services, leave registration to platform; this is unlikely
                }
                else
                {
                    builder.Services.AddSingleton<Services.IMedicationService, Services.MockMedicationService>();
                    builder.Services.AddSingleton<Services.IMedicationRepository, Services.InMemoryMedicationRepository>();
                    builder.Services.AddSingleton<Services.IMedicationLogRepository, Services.InMemoryMedicationLogRepository>();
                }
            }

        // HttpClient for API-backed services (uses IOptions to get API root URL)
        // Keep a general-purpose HttpClient for other services that need it
        builder.Services.AddSingleton<System.Net.Http.HttpClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Services.FeaturesOptions>>();
            return new System.Net.Http.HttpClient
            {
                BaseAddress = new System.Uri(options.Value.ApiRootUrl)
            };
        });

        // API client facade: interim HttpClient-backed implementation of the Kiota-generated client surface.
        builder.Services.AddSingleton<Services.IApiClientFacade, Services.ApiHttpClientFacade>();

        // Token handler and ApiInrService configured with a token-aware HttpClient
        builder.Services.AddSingleton<Services.ApiInrService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Services.FeaturesOptions>>();
            var auth = sp.GetRequiredService<Services.IAuthService>();
            var handlerLogger = sp.GetRequiredService<ILogger<Services.TokenDelegatingHandler>>();
            var apiLogger = sp.GetRequiredService<ILogger<Services.ApiInrService>>();

            var tokenHandler = new Services.TokenDelegatingHandler(auth, handlerLogger)
            {
                InnerHandler = new System.Net.Http.HttpClientHandler()
            };

            var client = new System.Net.Http.HttpClient(tokenHandler)
            {
                BaseAddress = new Uri(options.Value.ApiRootUrl)
            };

            return new Services.ApiInrService(client, apiLogger);
        });

        // OAuth configuration service - use configuration flag for mock/real
        builder.Services.AddSingleton<Services.IOAuthConfigService>(sp =>
        {
            return useMockServices
                ? (Services.IOAuthConfigService)new Services.MockOAuthConfigService()
                : new Services.OAuthConfigService(
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Services.FeaturesOptions>>(),
                    sp.GetRequiredService<System.Net.Http.HttpClient>(),
                    sp.GetRequiredService<ILogger<Services.OAuthConfigService>>(),
                    sp.GetRequiredService<Services.ICacheService>());
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
                    sp.GetRequiredService<ILogger<Services.AuthService>>(),
                    sp.GetRequiredService<System.Net.Http.HttpClient>());
        });

        // Background cache sync service: periodically fetch latest INR data and populate encrypted cache
        try
        {
            // Register the concrete CacheSyncService as a singleton and expose it as both
            // ICacheSyncWorker and IHostedService so platform jobs can invoke SyncOnceAsync
            builder.Services.AddSingleton<Services.CacheSyncService>();
            builder.Services.AddSingleton<Services.ICacheSyncWorker>(sp => sp.GetRequiredService<Services.CacheSyncService>());
            builder.Services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp => sp.GetRequiredService<Services.CacheSyncService>());

            // Development-only in-process worker: register DevSyncHostedService when debugging locally
            // or when the feature flag is explicitly enabled. This runs the same sync logic in-process
            // so you can iterate without packaging the app.
            var enableInProc = builder.Configuration["Features:EnableInProcBackground"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            if (System.Diagnostics.Debugger.IsAttached || enableInProc)
            {
                // Register the concrete dev worker as a singleton and map IHostedService
                // to the same instance so the host (if it starts hosted services) will
                // use the same instance and tests can resolve the concrete type.
                builder.Services.AddSingleton<Services.DevSyncHostedService>();
                builder.Services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp => sp.GetRequiredService<Services.DevSyncHostedService>());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MauiProgram: CacheSyncService/DevSyncHostedService registration failed: {ex.Message}");
        }

            // Platform-specific service registrations (implemented in platform files)
            RegisterPlatformServices(builder);

        var app = builder.Build();

        // Diagnostic: log which hosted services are registered and whether in-proc background
        // was requested. This helps debug cases where AddHostedService appears to be ignored.
        try
        {
            var startupLogger = app.Services.GetService<ILogger<App>>();
            var dbgAttached = System.Diagnostics.Debugger.IsAttached;
            var enableInProcFlag = builder.Configuration["Features:EnableInProcBackground"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

            var hostedSvcTypes = app.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
                .Select(s => s.GetType().FullName)
                .ToList();

            startupLogger?.LogInformation("Startup diagnostic: Debugger.IsAttached={Attached} EnableInProcBackground={EnableInProc} RegisteredHostedServices={Services}", dbgAttached, enableInProcFlag, string.Join(",", hostedSvcTypes));

            // If we're in development in-proc mode, ensure the dev hosted service is started.
            if (dbgAttached || enableInProcFlag)
            {
                try
                {
                    var dev = app.Services.GetService<Services.DevSyncHostedService>();
                    if (dev != null)
                    {
                        // Start in background to avoid blocking startup; StartAsync should be idempotent
                        _ = Task.Run(() => dev.StartAsync(System.Threading.CancellationToken.None));
                        startupLogger?.LogInformation("Startup: Triggered DevSyncHostedService.StartAsync()");
                    }
                }
                catch (Exception ex)
                {
                    startupLogger?.LogWarning(ex, "Failed to start DevSyncHostedService proactively");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MauiProgram: Startup diagnostic logging failed: {ex.Message}");
        }

        // Ensure SQLite database is created and migrations applied BEFORE any views load.
        // This must happen synchronously during startup to prevent "no such table" errors.
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>();
            if (db != null)
            {
                var startupLogger = scope.ServiceProvider.GetService<ILogger<App>>();
                startupLogger?.LogInformation("Startup: Applying EF Core migrations to local SQLite database");

                // Use synchronous migration to ensure DB is ready before app continues
                db.Database.Migrate();

                startupLogger?.LogInformation("Startup: Local SQLite database migrations applied successfully");

                // Also seed the mobile user
                var seeder = scope.ServiceProvider.GetService<Services.MobileUserSeeder>();
                seeder?.EnsureSeeded();
            }
        }
        catch (Exception ex)
        {
            // Log but don't crash - app can still function with API fallback
            var fallbackLogger = app.Services.GetService<ILogger<App>>();
            fallbackLogger?.LogError(ex, "Startup: Failed to apply database migrations");
        }

#if ANDROID
        // Initialize the Android service provider bridge so OS-created components
        // (JobService / BroadcastReceiver) can obtain scoped services safely.
        try
        {
            BloodThinnerTracker.Mobile.Platforms.Android.AndroidServiceProvider.Initialize(app.Services);
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILogger<App>>();
            logger?.LogError(ex, "Failed to initialize Android service provider bridge");
        }
#endif

#if WINDOWS
        // Initialize the Windows service provider bridge so OS-created background tasks
        // can obtain scoped services safely, and register the periodic background task.
        try
        {
            BloodThinnerTracker.Mobile.Platforms.Windows.WindowsServiceProvider.Initialize(app.Services);

            // Register background task for periodic INR data sync (idempotent - won't re-register if already done)
            var logger = app.Services.GetService<ILogger<App>>();
            BloodThinnerTracker.Mobile.Platforms.Windows.Background.WindowsSchedulingHelper.RegisterBackgroundTask(
                intervalMinutes: 15,
                force: false,
                store: new BloodThinnerTracker.Mobile.Platforms.Windows.Background.WindowsSchedulingFlagStore(),
                logger: logger);
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILogger<App>>();
            logger?.LogError(ex, "Failed to initialize Windows service provider or register background task");
        }
#endif



        return app;
    }

    // Partial method implemented in platform-specific files (no-op in shared code)
    static partial void RegisterPlatformServices(MauiAppBuilder builder);
}
