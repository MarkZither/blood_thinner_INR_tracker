using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BloodThinnerTracker.Mobile.Extensions;

/// <summary>
/// Extension methods for registering real API-backed services for production use.
/// Keeps MauiProgram.cs concise by encapsulating production service registration logic here.
/// </summary>
public static class RealServicesExtensions
{
    /// <summary>
    /// Registers all real API-backed services for production use.
    /// This includes authentication, INR, medication, and sync services.
    /// </summary>
    public static IServiceCollection AddRealServices(this IServiceCollection services, IConfiguration configuration, bool skipPlatformServices)
    {
        // Real authentication and OAuth
        services.AddSingleton<Services.IAuthService>(sp =>
            new Services.AuthService(
                sp.GetRequiredService<Services.ISecureStorageService>(),
                sp.GetRequiredService<Services.IOAuthConfigService>(),
                sp.GetRequiredService<IOptions<Services.FeaturesOptions>>(),
                sp.GetRequiredService<ILogger<Services.AuthService>>(),
                sp.GetRequiredService<System.Net.Http.HttpClient>()));

        services.AddSingleton<Services.IOAuthConfigService>(sp =>
            new Services.OAuthConfigService(
                sp.GetRequiredService<IOptions<Services.FeaturesOptions>>(),
                sp.GetRequiredService<System.Net.Http.HttpClient>(),
                sp.GetRequiredService<ILogger<Services.OAuthConfigService>>(),
                sp.GetRequiredService<Services.ICacheService>()));

        // Real INR service (API-backed with token authentication)
        services.AddSingleton<Services.ApiInrService>(sp =>
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
        services.AddSingleton<Services.IInrService>(sp => sp.GetRequiredService<Services.ApiInrService>());

        // Real medication service (API-backed)
        services.AddSingleton<Services.IMedicationService, Services.ApiMedicationService>();

        // Real API client facade (uses Kiota-generated client)
        services.AddSingleton<Services.IApiClientFacade, Services.ApiHttpClientFacade>();

        // Real foreground sync service
        services.AddSingleton<Services.IForegroundSyncService, Services.ForegroundSyncService>();

        // Platform-dependent repository registration
        if (!skipPlatformServices)
        {
            services.AddPlatformDatabaseServices();
        }
        else
        {
            // Test-mode: use in-memory repositories as fallback
            services.AddSingleton<Services.IInrRepository, Services.InMemoryInrRepository>();
            services.AddSingleton<Services.IMedicationRepository, Services.InMemoryMedicationRepository>();
            services.AddSingleton<Services.IMedicationLogRepository, Services.InMemoryMedicationLogRepository>();
        }

        return services;
    }

    /// <summary>
    /// Registers platform-specific database services (SQLite, Data Protection, EF Core).
    /// Should only be called on actual device/emulator, not in test mode.
    /// </summary>
    public static IServiceCollection AddPlatformDatabaseServices(this IServiceCollection services)
    {
        try
        {
            // Data Protection for encryption
            var dataProtectionPath = System.IO.Path.Combine(
                Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "DataProtection-Keys");
            services.AddDataProtection()
                .PersistKeysToFileSystem(new System.IO.DirectoryInfo(dataProtectionPath))
                .SetApplicationName("BloodThinnerTracker.Mobile");

            // Mobile-specific user service
            services.AddScoped<Services.MobileUserSeeder>();
            services.AddScoped<BloodThinnerTracker.Data.Shared.ICurrentUserService, Services.MobileCurrentUserService>();

            // SQLite-backed repositories
            services.AddScoped<Services.IInrRepository, Services.InrRepository>();
            services.AddScoped<Services.IMedicationRepository, Services.MedicationRepository>();
            services.AddScoped<Services.IMedicationLogRepository, Services.MedicationLogRepository>();

            // EF Core DbContext
            var dbPath = System.IO.Path.Combine(
                Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "bloodtracker_mobile.db");
            services.AddDbContext<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>(opts =>
            {
                opts.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("BloodThinnerTracker.Data.SQLite"));
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddPlatformDatabaseServices: SQLite/DataProtection registration failed: {ex.Message}");
        }

        return services;
    }
}
