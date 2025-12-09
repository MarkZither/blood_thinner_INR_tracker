using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Android.App;
using Android.Content;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Mobile.Platforms.Android;

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

                    // Also reschedule individual reminders persisted in the JSON store (T036/T037)
                    try
                    {
                        var storePath = Path.Combine(FileSystem.AppDataDirectory, "scheduled_reminders.json");
                        if (File.Exists(storePath))
                        {
                            var txt = File.ReadAllText(storePath);
                            if (!string.IsNullOrWhiteSpace(txt))
                            {
                                var list = JsonSerializer.Deserialize<List<NotificationInfo>>(txt) ?? new List<NotificationInfo>();
                                var scheduler = new NotificationSchedulerAndroid();
                                var alarmScheduler = new NotificationSchedulerAlarm();

                                foreach (var info in list.Where(i => i.IsScheduled && i.TriggerTime > DateTimeOffset.UtcNow))
                                {
                                    var req = new NotificationRequest(info.Id, info.TriggerTime, info.Title, info.Body, null);
                                    if (!string.IsNullOrEmpty(info.AlarmType))
                                    {
                                        // Reschedule exact alarm path
                                        _ = alarmScheduler.ScheduleExactAsync(req);
                                    }
                                    else
                                    {
                                        // Reschedule plugin-managed notification
                                        _ = scheduler.ScheduleAsync(req);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception exReschedule)
                    {
                        try { global::Android.Util.Log.Warn("BootCompletedReceiver", exReschedule.ToString()); } catch { }
                    }
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
