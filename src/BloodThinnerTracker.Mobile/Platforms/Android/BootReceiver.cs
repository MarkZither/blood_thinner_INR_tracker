 #if ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Android.App;
using Android.Content;
using Microsoft.Maui.Storage;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.Platforms.Android
{
    [BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        private const string StoreFileName = "scheduled_reminders.json";

        public override void OnReceive(Context? context, Intent? intent)
        {
            try
            {
                // Only handle boot completed actions
                if (intent?.Action == null) return;

                if (intent.Action != Intent.ActionBootCompleted && intent.Action != Intent.ActionLockedBootCompleted)
                    return;

                var storePath = Path.Combine(FileSystem.AppDataDirectory, StoreFileName);
                if (!File.Exists(storePath)) return;

                var txt = File.ReadAllText(storePath);
                if (string.IsNullOrWhiteSpace(txt)) return;

                var list = JsonSerializer.Deserialize<List<NotificationInfo>>(txt) ?? new List<NotificationInfo>();
                var scheduler = new NotificationSchedulerAndroid();

                // Re-schedule any reminders that were marked scheduled and whose trigger time is in the future
                foreach (var info in list.Where(i => i.IsScheduled && i.TriggerTime > DateTimeOffset.UtcNow))
                {
                    var req = new NotificationRequest(info.Id, info.TriggerTime, info.Title, info.Body, null);
                    // fire-and-forget — the scheduler handles persistence
                    _ = scheduler.ScheduleAsync(req);
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Warn("BootReceiver", ex.ToString());
            }
        }
    }
}

#endif
