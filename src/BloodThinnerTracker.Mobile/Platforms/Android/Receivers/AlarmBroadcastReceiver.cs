using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Android.App;
using Android.Content;
using Plugin.LocalNotification;
using Microsoft.Maui.Storage;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.Platforms.Android.Receivers
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { "com.markzither.bloodthinnertracker.ACTION_REMINDER" })]
    public class AlarmBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            try
            {
                if (intent == null || context == null) return;

                var idStr = intent.GetStringExtra("reminderId");
                if (string.IsNullOrEmpty(idStr)) return;

                if (!Guid.TryParse(idStr, out var id)) return;

                // Read store to find metadata (best-effort)
                var storePath = Path.Combine(FileSystem.AppDataDirectory, "scheduled_reminders.json");
                NotificationInfo? info = null;
                if (File.Exists(storePath))
                {
                    var txt = File.ReadAllText(storePath);
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        var list = JsonSerializer.Deserialize<System.Collections.Generic.List<NotificationInfo>>(txt);
                        info = list?.FirstOrDefault(x => x.Id == id);
                    }
                }

                // Compose a notification request from stored info or intent extras
                var title = intent.GetStringExtra("title") ?? info?.Title ?? "Reminder";
                var body = intent.GetStringExtra("body") ?? info?.Body ?? string.Empty;

                var request = new Plugin.LocalNotification.NotificationRequest
                {
                    NotificationId = id.GetHashCode(),
                    Title = title,
                    Description = body,
                    // Fire immediately
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = DateTime.Now
                    },
                };

                LocalNotificationCenter.Current.Show(request);
            }
            catch (Exception ex)
            {
                try { global::Android.Util.Log.Warn("AlarmBroadcastReceiver", ex.ToString()); } catch { }
            }
        }
    }
}
