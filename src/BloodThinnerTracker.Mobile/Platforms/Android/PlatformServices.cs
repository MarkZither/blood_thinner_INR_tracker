#if ANDROID
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
        }
    }
}
#endif
