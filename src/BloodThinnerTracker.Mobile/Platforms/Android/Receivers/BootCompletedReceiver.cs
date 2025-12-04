using System;
using Android.App;
using Android.Content;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Platforms.Android.Receivers
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted })]
    public class BootCompletedReceiver : BroadcastReceiver
    {
        // Default interval to reschedule the periodic job (15 minutes)
        const long DefaultIntervalMillis = 15 * 60 * 1000;

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent == null) return;

            var action = intent.Action;
            if (action == Intent.ActionBootCompleted || action == Intent.ActionLockedBootCompleted)
            {
                try
                {
                    // Reschedule the periodic job. ForegroundServiceHelper lives in the Services namespace.
                    BloodThinnerTracker.Mobile.Platforms.Android.Services.ForegroundServiceHelper.SchedulePeriodicJob(context, DefaultIntervalMillis);
                }
                catch (Exception ex)
                {
                    // Try to log via the DI logger if available, otherwise use Android.Util.Log
                    try
                    {
                        var scope = BloodThinnerTracker.Mobile.Platforms.Android.AndroidServiceProvider.CreateScope();
                        var logger = scope.ServiceProvider.GetService(typeof(Microsoft.Extensions.Logging.ILogger<BootCompletedReceiver>)) as Microsoft.Extensions.Logging.ILogger<BootCompletedReceiver>;
                        logger?.LogWarning(ex, "BootCompletedReceiver: failed to schedule periodic job");
                    }
                    catch
                    {
                        try { global::Android.Util.Log.Warn("BootCompletedReceiver", ex.ToString()); } catch { }
                    }
                }
            }
        }
    }
}
