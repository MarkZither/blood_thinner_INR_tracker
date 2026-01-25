using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BloodThinnerTracker.Mobile.Extensions;

/// <summary>
/// Extension methods for registering shared infrastructure services used by both mock and real modes.
/// Keeps MauiProgram.cs concise by encapsulating infrastructure registration logic here.
/// </summary>
public static class SharedInfrastructureExtensions
{
    /// <summary>
    /// Registers core singleton services used throughout the app (theme, secure storage, encryption, etc.).
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<App>();
        services.AddSingleton<AppShell>();
        services.AddSingleton<Services.IThemeService, Services.ThemeService>();
        services.AddSingleton<MainPage>();
        services.AddSingleton<Services.ISecureStorageService, Services.SecureStorageService>();
        services.AddSingleton<Services.EncryptionService>();
        services.AddSingleton<Services.ICacheService, Services.CacheService>();
        services.AddSingleton<Services.IReminderComputationService, Services.ReminderComputationService>();
        services.AddSingleton<Services.IAppInitializer, Services.AppInitializer>();

        return services;
    }

    /// <summary>
    /// Registers views and viewmodels for navigation.
    /// </summary>
    public static IServiceCollection AddViewsAndViewModels(this IServiceCollection services)
    {
        services.AddTransient<Views.LoginView>();
        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<Views.InrListView>();
        services.AddTransient<ViewModels.InrListViewModel>();
        services.AddTransient<ViewModels.MedicineListViewModel>();
        services.AddTransient(typeof(LazyViewModelFactory<>));
        services.AddTransient<Views.AboutView>();
        services.AddTransient<Views.MedicineListView>();
        services.AddTransient<Views.DebugScheduledRemindersPage>();
        services.AddTransient<Pages.SettingsPage>();

        return services;
    }

    /// <summary>
    /// Registers the HttpClient configured with API base address from FeaturesOptions.
    /// </summary>
    public static IServiceCollection AddApiHttpClient(this IServiceCollection services)
    {
        services.AddSingleton<System.Net.Http.HttpClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Services.FeaturesOptions>>();
            return new System.Net.Http.HttpClient
            {
                BaseAddress = new Uri(options.Value.ApiRootUrl)
            };
        });

        return services;
    }

    /// <summary>
    /// Registers background and hosted services for cache sync.
    /// </summary>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            services.AddSingleton<Services.CacheSyncService>();
            services.AddSingleton<Services.ICacheSyncWorker>(sp => sp.GetRequiredService<Services.CacheSyncService>());
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp => sp.GetRequiredService<Services.CacheSyncService>());

            var enableInProc = configuration["Features:EnableInProcBackground"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            if (System.Diagnostics.Debugger.IsAttached || enableInProc)
            {
                services.AddSingleton<Services.DevSyncHostedService>();
                services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp => sp.GetRequiredService<Services.DevSyncHostedService>());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddBackgroundServices: CacheSyncService/DevSyncHostedService registration failed: {ex.Message}");
        }

        return services;
    }
}
