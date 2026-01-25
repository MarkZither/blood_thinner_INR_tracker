#if ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;
using BloodThinnerTracker.Mobile.Services;
using AppNotificationRequest = BloodThinnerTracker.Mobile.Services.NotificationRequest;
using PluginNotificationRequest = Plugin.LocalNotification.NotificationRequest;

namespace BloodThinnerTracker.Mobile.Platforms.Android
{
    /// <summary>
    /// Android implementation of INotificationScheduler using Plugin.LocalNotification.
    /// This avoids the complex AndroidX WorkManager dependency conflicts.
    /// </summary>
    public class NotificationSchedulerAndroid : INotificationScheduler
    {
        private const string StoreFileName = "scheduled_reminders.json";
        private const string ReminderChannelId = "bloodthinner_reminders";
        private const string ReminderChannelName = "Medication Reminders";
        private const string ReminderChannelDescription = "Critical medication reminder notifications";
        private readonly string _storePath;
        private static bool _channelCreated = false;

        /// <summary>
        /// Public accessor for the reminder channel ID for use by other scheduler implementations.
        /// </summary>
        public static string ReminderChannelIdPublic => ReminderChannelId;

        public NotificationSchedulerAndroid()
        {
            _storePath = Path.Combine(FileSystem.AppDataDirectory, StoreFileName);
            EnsureNotificationChannelCreated();
        }

        /// <summary>
        /// Ensures the notification channel for medication reminders is created with high importance.
        /// This is required for Android 8.0+ (API 26+) to deliver notifications.
        /// </summary>
        public static void EnsureNotificationChannelCreated()
        {
            if (_channelCreated) return;

            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    var context = global::Android.App.Application.Context;
                    var nm = context.GetSystemService(global::Android.Content.Context.NotificationService) as NotificationManager;
                    if (nm == null) return;

                    var existingChannel = nm.GetNotificationChannel(ReminderChannelId);
                    if (existingChannel == null)
                    {
                        // Create channel with HIGH importance for medication reminders
                        // High importance ensures: makes sound, appears as heads-up notification
                        var channel = new NotificationChannel(ReminderChannelId, ReminderChannelName, NotificationImportance.High)
                        {
                            Description = ReminderChannelDescription
                        };

                        // Enable lights, vibration, and badge for medication reminders
                        channel.EnableLights(true);
                        channel.EnableVibration(true);
                        channel.SetShowBadge(true);

                        // Set bypass DND if possible (user can override in settings)
                        channel.SetBypassDnd(false); // Respect user's DND preference

                        nm.CreateNotificationChannel(channel);
                        System.Diagnostics.Debug.WriteLine($"NotificationSchedulerAndroid: Created notification channel '{ReminderChannelId}' with High importance");
                    }

                    _channelCreated = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerAndroid: Failed to create notification channel: {ex}");
            }
        }

        public async Task ScheduleAsync(AppNotificationRequest request)
        {
            System.Diagnostics.Debug.WriteLine($"NotificationSchedulerAndroid: ScheduleAsync enter (id={request.Id})");
            // Persist schedule for query purposes — guard against a hung ReadStoreAsync
            var readTask = ReadStoreAsync();
            var completed = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
            List<NotificationInfo> list;
            if (completed == readTask)
            {
                list = await readTask.ConfigureAwait(false);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerAndroid: ScheduleAsync ReadStoreAsync timed out for id={request.Id}");
                list = new List<NotificationInfo>();
            }
            list.RemoveAll(x => x.Id == request.Id);
            list.Add(new NotificationInfo(request.Id, request.TriggerTime, request.Title, request.Body, true));
            await WriteStoreAsync(list).ConfigureAwait(false);

            // Schedule with Plugin.LocalNotification
            var notificationRequest = new PluginNotificationRequest
            {
                NotificationId = request.Id.GetHashCode(),
                Title = request.Title,
                Description = request.Body,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = request.TriggerTime.LocalDateTime
                },
                Android = new Plugin.LocalNotification.AndroidOption.AndroidOptions
                {
                    ChannelId = ReminderChannelId,
                    Priority = Plugin.LocalNotification.AndroidOption.AndroidPriority.High
                }
            };

            // Ensure notifications are allowed / request permission on Android 13+
            try
            {
                if (!NotificationPermissionHelper.AreNotificationsEnabled())
                {
                    // Request runtime permission or open settings as fallback
                    NotificationPermissionHelper.RequestPostNotificationsIfNeeded();
                    System.Diagnostics.Debug.WriteLine($"NotificationSchedulerAndroid: Notifications disabled or permission missing for id={request.Id}");
                }
            }
            catch (Exception exPerm)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerAndroid: Permission check/request failed: {exPerm}");
            }

            await LocalNotificationCenter.Current.Show(notificationRequest).ConfigureAwait(false);
        }

        public async Task CancelAsync(Guid reminderId)
        {
            // Cancel via Plugin.LocalNotification
            LocalNotificationCenter.Current.Cancel(reminderId.GetHashCode());
            var readTask = ReadStoreAsync();
            var completed = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
            var list = completed == readTask ? await readTask.ConfigureAwait(false) : new List<NotificationInfo>();
            var removed = list.RemoveAll(x => x.Id == reminderId);
            if (removed > 0) await WriteStoreAsync(list).ConfigureAwait(false);
        }

        public async Task<NotificationInfo?> GetScheduledAsync(Guid reminderId)
        {
            var readTask = ReadStoreAsync();
            var completed = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
            var list = completed == readTask ? await readTask.ConfigureAwait(false) : new List<NotificationInfo>();
            return list.FirstOrDefault(x => x.Id == reminderId);
        }

        public async Task<IEnumerable<NotificationInfo>> GetAllScheduledAsync()
        {
            var readTask = ReadStoreAsync();
            var completed = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
            var list = completed == readTask ? await readTask.ConfigureAwait(false) : new List<NotificationInfo>();
            return list.AsReadOnly();
        }

        private async Task<List<NotificationInfo>> ReadStoreAsync()
        {
            try
            {
                if (!File.Exists(_storePath)) return new List<NotificationInfo>();

                // Read with shared access and a timeout to avoid hanging when the file is locked
                using var fs = new FileStream(_storePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
                using var sr = new StreamReader(fs);

                var readTask = sr.ReadToEndAsync();
                var completed = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
                if (completed != readTask)
                {
                    // Timed out reading the file — return empty list to avoid blocking caller
                    System.Diagnostics.Debug.WriteLine("NotificationSchedulerAndroid: ReadStoreAsync timed out reading store file.");
                    return new List<NotificationInfo>();
                }

                var txt = await readTask.ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(txt)) return new List<NotificationInfo>();
                return JsonSerializer.Deserialize<List<NotificationInfo>>(txt) ?? new List<NotificationInfo>();
            }
            catch (Exception ex)
            {
                // Log and return empty list — best-effort persistence
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerAndroid: ReadStoreAsync failed: {ex}");
                return new List<NotificationInfo>();
            }
        }

        private async Task WriteStoreAsync(List<NotificationInfo> list)
        {
            try
            {
                var txt = JsonSerializer.Serialize(list);
                var tmpPath = _storePath + ".tmp";

                // Write to a temp file then replace to reduce chance of file lock conflicts
                var bytes = System.Text.Encoding.UTF8.GetBytes(txt);
                using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    await fs.FlushAsync().ConfigureAwait(false);
                }

                // Replace the original file
                try
                {
                    File.Copy(tmpPath, _storePath, overwrite: true);
                    File.Delete(tmpPath);
                }
                catch (Exception exReplace)
                {
                    System.Diagnostics.Debug.WriteLine($"NotificationSchedulerAndroid: WriteStoreAsync replace failed: {exReplace}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerAndroid: WriteStoreAsync failed: {ex}");
                // best-effort persistence
            }
        }
    }
}
#endif
