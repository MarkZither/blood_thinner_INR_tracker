using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Mobile.ViewModels;
using BloodThinnerTracker.Mobile.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;

namespace BloodThinnerTracker.Mobile;

/// <summary>
/// MAUI Application entry point for Blood Thinner Tracker
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("FontAwesome6Free-Regular-400.otf", "FontAwesomeRegular");
                fonts.AddFont("FontAwesome6Free-Solid-900.otf", "FontAwesomeSolid");
            });

        // Register services
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        builder.Services.AddSingleton<INotificationService, NotificationService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<IBiometricService, BiometricService>();

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<MedicationViewModel>();
        builder.Services.AddTransient<INRTrackingViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        // Register Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<MedicationPage>();
        builder.Services.AddTransient<INRTrackingPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ProfilePage>();

        // HTTP Client configuration
        builder.Services.AddHttpClient("BloodThinnerApi", client =>
        {
            client.BaseAddress = new Uri("https://localhost:5001/api/");
            client.DefaultRequestHeaders.Add("User-Agent", "BloodThinnerTracker-Mobile/1.0");
        });

        // Configure logging
#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
        builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif

        // Platform-specific configurations
        ConfigurePlatformServices(builder);

        return builder.Build();
    }

    private static void ConfigurePlatformServices(MauiAppBuilder builder)
    {
#if ANDROID
        builder.Services.AddSingleton<Platforms.Android.Services.AndroidNotificationService>();
#elif IOS
        builder.Services.AddSingleton<Platforms.iOS.Services.iOSNotificationService>();
#elif WINDOWS
        builder.Services.AddSingleton<Platforms.Windows.Services.WindowsNotificationService>();
#endif
    }
}