using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Plugin.LocalNotification;

namespace BloodThinnerTracker.Mobile
{
    public static partial class MauiProgram
    {
        static partial void RegisterPlatformServices(MauiAppBuilder builder)
        {
            // Initialize Plugin.LocalNotification on Android and create the notification channel
            try
            {
                // Ensure Plugin.LocalNotification is referenced/initialized on Android
                // The plugin handles channel creation internally; touching Current forces the native platform
                // initialization path to run without introducing cross-TFM calls in shared startup.
                var center = LocalNotificationCenter.Current;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlatformServices(Android): Failed to initialize LocalNotification: {ex}");
            }

            // Android platform registrations
            builder.Services.AddSingleton<Services.INotificationScheduler, Platforms.Android.NotificationSchedulerAndroid>();
            builder.Services.AddSingleton<Platforms.Android.NotificationSchedulerAlarm>();
            // Perform best-effort runtime initialization that requires IServiceProvider by
            // building a temporary provider from the collection. This avoids putting
            // platform initialization into shared startup while also avoiding a
            // separate IPlatformInitializer type which caused multi-target compile issues.
            try
            {
                using var sp = builder.Services.BuildServiceProvider();
                try
                {
                    BloodThinnerTracker.Mobile.Platforms.Android.AndroidServiceProvider.Initialize(sp);
                }
                catch (Exception ex)
                {
                    var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger>();
                    logger?.LogError(ex, "PlatformServices(Android): AndroidServiceProvider.Initialize failed");
                }
            }
            catch { /* ignore best-effort init failures during registration */ }
        }
    }
}

// Runtime initializer that bridges OS-created Android components to the DI container
// No separate runtime initializer type needed; initialization attempted above.
