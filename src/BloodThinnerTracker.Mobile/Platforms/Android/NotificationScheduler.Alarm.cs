// Clean plugin-backed NotificationSchedulerAlarm implementation
#if ANDROID
using System.Threading.Tasks;
using Android.OS;
using Plugin.LocalNotification;
using BloodThinnerTracker.Mobile.Services;
using AppNotificationRequest = BloodThinnerTracker.Mobile.Services.NotificationRequest;
using PluginNotificationRequest = Plugin.LocalNotification.NotificationRequest;

namespace BloodThinnerTracker.Mobile.Platforms.Android
{
    /// <summary>
    /// Plugin-backed scheduler for high-priority reminders. Uses Plugin.LocalNotification
    /// which will leverage AlarmManager internally when exact alarms are available.
    /// </summary>
    public class NotificationSchedulerAlarm
    {
        public bool CanScheduleExactAlarms()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    var amObj = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.AlarmService);
                    var am = amObj as global::Android.App.AlarmManager;
                    return am?.CanScheduleExactAlarms() ?? false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task ScheduleExactAsync(AppNotificationRequest request)
        {
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
                    ChannelId = "bloodthinner_reminders",
                    Priority = Plugin.LocalNotification.AndroidOption.AndroidPriority.Max
                }
            };

            await LocalNotificationCenter.Current.Show(notificationRequest).ConfigureAwait(false);
        }
    }
}
#endif
