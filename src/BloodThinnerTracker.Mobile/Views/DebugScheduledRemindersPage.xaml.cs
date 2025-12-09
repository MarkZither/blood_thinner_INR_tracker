using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BloodThinnerTracker.Mobile.Views
{
    public partial class DebugScheduledRemindersPage : ContentPage
    {
        private readonly Services.INotificationScheduler _scheduler;
        private readonly IServiceProvider _services;

        public DebugScheduledRemindersPage(Services.INotificationScheduler scheduler, IServiceProvider services)
        {
            InitializeComponent();
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _services = services ?? throw new ArgumentNullException(nameof(services));

            BtnScheduleWork.Clicked += async (_, __) => await ScheduleInTwoMinutes();
            BtnScheduleExact.Clicked += async (_, __) => await ScheduleExactAlarmInOneMinute();
            BtnEnsurePermissions.Clicked += (_, __) => EnsurePermissions();
            BtnList.Clicked += async (_, __) => await GetScheduled();
        }

        // Parameterless ctor for Shell DataTemplate activation; bridge to DI
        public DebugScheduledRemindersPage()
            : this(
                  App.ServiceProvider?.GetRequiredService<Services.INotificationScheduler>() ?? throw new InvalidOperationException("ServiceProvider not initialized"),
                  App.ServiceProvider ?? throw new InvalidOperationException("ServiceProvider not initialized"))
        {
        }

        private void SetMessage(string text)
        {
            MainThread.BeginInvokeOnMainThread(() => LblMessage.Text = text);
        }

        private async Task ScheduleInTwoMinutes()
        {
            try
            {
                var permType = Type.GetType("BloodThinnerTracker.Mobile.Platforms.Android.NotificationPermissionHelper");
                if (permType != null)
                {
                    var areMethod = permType.GetMethod("AreNotificationsEnabled", BindingFlags.Public | BindingFlags.Static);
                    if (areMethod != null)
                    {
                        var enabled = areMethod.Invoke(null, null) as bool? ?? false;
                        if (!enabled)
                        {
                            // Request permission if not enabled (will open prompt/settings)
                            var reqMethod = permType.GetMethod("RequestPostNotificationsIfNeeded", BindingFlags.Public | BindingFlags.Static);
                            reqMethod?.Invoke(null, null);
                            SetMessage("Notifications appear disabled. Requested permission; please enable notifications for the app.");
                            // continue to schedule so user can test after granting
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessage($"Permission check failed: {ex.Message}");
            }

            var id = Guid.NewGuid();
            var req = new Services.NotificationRequest(id, DateTimeOffset.UtcNow.AddMinutes(2), "Test Reminder", "This is a test reminder (WorkManager)");
            await _scheduler.ScheduleAsync(req).ConfigureAwait(false);
            SetMessage($"Scheduled (WorkManager) reminder {id} in 2 minutes.");
        }

        private void EnsurePermissions()
        {
            try
            {
                var permType = Type.GetType("BloodThinnerTracker.Mobile.Platforms.Android.NotificationPermissionHelper");
                if (permType == null)
                {
                    SetMessage("Notification permission helper not available on this platform.");
                    return;
                }

                var areMethod = permType.GetMethod("AreNotificationsEnabled", BindingFlags.Public | BindingFlags.Static);
                var enabled = true;
                if (areMethod != null)
                {
                    enabled = areMethod.Invoke(null, null) as bool? ?? true;
                }

                if (enabled)
                {
                    SetMessage("Notifications are enabled for this app.");
                    return;
                }

                // Not enabled - try request
                var reqMethod = permType.GetMethod("RequestPostNotificationsIfNeeded", BindingFlags.Public | BindingFlags.Static);
                reqMethod?.Invoke(null, null);
                SetMessage("Requested notification permission (or opened settings). If still blocked, please enable notifications for the app.");
            }
            catch (Exception ex)
            {
                SetMessage($"Failed to request/check permission: {ex.Message}");
            }
        }

        private async Task ScheduleExactAlarmInOneMinute()
        {
            var t = Type.GetType("BloodThinnerTracker.Mobile.Platforms.Android.NotificationSchedulerAlarm");
            if (t == null)
            {
                SetMessage("Exact alarm scheduler not available on this platform.");
                return;
            }

            var alarm = _services.GetService(t);
            if (alarm == null)
            {
                SetMessage("Exact alarm scheduler not available on this platform.");
                return;
            }

            var id = Guid.NewGuid();
            var req = new Services.NotificationRequest(id, DateTimeOffset.UtcNow.AddMinutes(1), "Exact Alarm Reminder", "This is a test exact alarm reminder");
            try
            {
                var mi = t.GetMethod("ScheduleExactAsync", BindingFlags.Public | BindingFlags.Instance);
                if (mi == null)
                {
                    SetMessage("Exact alarm scheduler does not expose ScheduleExactAsync.");
                    return;
                }

                var taskObj = mi.Invoke(alarm, new object[] { req });
                if (taskObj is System.Threading.Tasks.Task task)
                {
                    await task.ConfigureAwait(false);
                }
                else
                {
                    SetMessage("Exact alarm scheduler invocation did not return a Task.");
                    return;
                }
            }
            catch (Exception ex)
            {
                SetMessage($"Failed to schedule exact alarm: {ex.Message}");
                return;
            }

            SetMessage($"Scheduled exact alarm reminder {id} in 1 minute.");
        }

        private async Task GetScheduled()
        {
            var list = await _scheduler.GetAllScheduledAsync().ConfigureAwait(false);
            SetMessage($"Scheduled: {string.Join(", ", list.Select(i => i.Id.ToString()))}");
        }
    }
}
