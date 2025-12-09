using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Android.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace BloodThinnerTracker.Mobile.Platforms.Android
{
    [Activity(Name = "com.markzither.bloodthinnertracker.MainActivity", Theme = "@style/Maui.SplashTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : Microsoft.Maui.MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Register global exception handlers early to capture startup crashes
            RegisterGlobalExceptionHandlers();

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

        private void RegisterGlobalExceptionHandlers()
        {
            try
            {
                AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
                {
                    try { LogUnhandled("AndroidEnvironment.UnhandledExceptionRaiser", args.Exception); } catch { }
                };

                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    try { LogUnhandled("AppDomain.UnhandledException", e.ExceptionObject as Exception); } catch { }
                };

                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    try { LogUnhandled("TaskScheduler.UnobservedTaskException", e.Exception); e.SetObserved(); } catch { }
                };
            }
            catch (Exception ex)
            {
                try { global::Android.Util.Log.Warn("MainActivity", "Failed to register global exception handlers: " + ex); } catch { }
            }
        }

        private void LogUnhandled(string tag, Exception? ex)
        {
            if (ex == null) return;
            try
            {
                var scope = BloodThinnerTracker.Mobile.Platforms.Android.AndroidServiceProvider.CreateScope();
                var logger = scope.ServiceProvider.GetService(typeof(ILogger<MainActivity>)) as ILogger<MainActivity>;
                if (logger != null)
                {
                    logger.LogError(ex, "{Tag}: Unhandled exception", tag);
                    return;
                }
            }
            catch { }

            try { global::Android.Util.Log.Error(tag, ex.ToString()); } catch { }
        }
    }
}
