using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Platforms.Android
{
    [Activity(Name = "com.markzither.bloodthinnertracker.MainActivity", Theme = "@style/Maui.SplashTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : Microsoft.Maui.MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                // Schedule the periodic job (15 minutes) on first run. Use a try/catch
                // because scheduling APIs may not be available on some emulators.
                var interval = (long)TimeSpan.FromMinutes(15).TotalMilliseconds;
                Services.ForegroundServiceHelper.SchedulePeriodicJob(global::Android.App.Application.Context, interval);
            }
            catch (Exception ex)
            {
                // Try to log via DI logger if available; otherwise fallback to Android.Util.Log
                try
                {
                    var scope = BloodThinnerTracker.Mobile.Platforms.Android.AndroidServiceProvider.CreateScope();
                    var logger = scope.ServiceProvider.GetService(typeof(Microsoft.Extensions.Logging.ILogger<MainActivity>)) as Microsoft.Extensions.Logging.ILogger<MainActivity>;
                    logger?.LogWarning(ex, "MainActivity: failed to schedule periodic job");
                }
                catch
                {
                    try { global::Android.Util.Log.Warn("MainActivity", ex.ToString()); } catch { }
                }
            }
        }
    }
}
