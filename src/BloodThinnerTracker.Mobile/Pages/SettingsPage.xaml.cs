using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.Pages
{
    /// <summary>
    /// Settings page that provides notification permission guidance, sync controls,
    /// and debugging tools for the mobile app.
    /// </summary>
    public partial class SettingsPage : ContentPage
    {
        private readonly INotificationScheduler? _notificationScheduler;
        private readonly ICacheSyncWorker? _syncWorker;
        private readonly ICacheService? _cacheService;

        public SettingsPage(
            INotificationScheduler? notificationScheduler = null,
            ICacheSyncWorker? syncWorker = null,
            ICacheService? cacheService = null)
        {
            InitializeComponent();
            _notificationScheduler = notificationScheduler;
            _syncWorker = syncWorker;
            _cacheService = cacheService;

            // Wire up button events
            BtnRequestNotifications.Clicked += async (_, __) => await RequestNotificationPermissionAsync();
            BtnOpenNotificationSettings.Clicked += (_, __) => OpenNotificationSettings();
            BtnRequestExactAlarm.Clicked += (_, __) => RequestExactAlarmPermission();
            BtnOpenBatterySettings.Clicked += (_, __) => OpenBatterySettings();
            BtnSyncNow.Clicked += async (_, __) => await SyncNowAsync();
            BtnViewScheduledReminders.Clicked += async (_, __) => await ViewScheduledRemindersAsync();
        }

        /// <summary>
        /// Parameterless constructor for Shell navigation DataTemplate support.
        /// </summary>
        public SettingsPage()
            : this(
                  App.ServiceProvider?.GetService<INotificationScheduler>(),
                  App.ServiceProvider?.GetService<ICacheSyncWorker>(),
                  App.ServiceProvider?.GetService<ICacheService>())
        {
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefreshStatusAsync();
        }

        private async Task RefreshStatusAsync()
        {
            await CheckNotificationStatusAsync();
            await CheckExactAlarmStatusAsync();
            await CheckLastSyncAsync();
            await LoadDiagnosticsAsync();
        }

        private async Task CheckNotificationStatusAsync()
        {
            try
            {
                bool areEnabled = await CheckNotificationsEnabledAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (areEnabled)
                    {
                        LblNotificationStatus.Text = "Enabled ✓";
                        LblNotificationStatus.TextColor = Colors.Green;
                        LblNotificationGuidance.IsVisible = false;
                        BtnRequestNotifications.IsVisible = false;
                        BtnOpenNotificationSettings.IsVisible = false;
                    }
                    else
                    {
                        LblNotificationStatus.Text = "Disabled ✗";
                        LblNotificationStatus.TextColor = Colors.Red;
                        LblNotificationGuidance.Text = "Notifications are disabled. Medication reminders will not be delivered. Please enable notifications to receive important medication reminders.";
                        LblNotificationGuidance.IsVisible = true;
                        BtnRequestNotifications.IsVisible = true;
                        BtnOpenNotificationSettings.IsVisible = true;
                    }
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LblNotificationStatus.Text = "Unknown";
                    LblNotificationStatus.TextColor = Colors.Orange;
                    LblNotificationGuidance.Text = $"Could not check notification status: {ex.Message}";
                    LblNotificationGuidance.IsVisible = true;
                });
            }
        }

        private async Task<bool> CheckNotificationsEnabledAsync()
        {
            // Try to use the platform-specific helper on Android
            try
            {
                var permType = Type.GetType("BloodThinnerTracker.Mobile.Platforms.Android.NotificationPermissionHelper");
                if (permType != null)
                {
                    var areMethod = permType.GetMethod("AreNotificationsEnabled", BindingFlags.Public | BindingFlags.Static);
                    if (areMethod != null)
                    {
                        return areMethod.Invoke(null, null) as bool? ?? true;
                    }
                }
            }
            catch { }

            // Fallback: assume enabled on other platforms
            return await Task.FromResult(true);
        }

        private async Task CheckExactAlarmStatusAsync()
        {
            try
            {
                // Only show exact alarm section on Android
                var alarmType = Type.GetType("BloodThinnerTracker.Mobile.Platforms.Android.NotificationSchedulerAlarm");
                if (alarmType == null)
                {
                    // Not on Android - hide exact alarm section
                    MainThread.BeginInvokeOnMainThread(() => BorderExactAlarm.IsVisible = false);
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() => BorderExactAlarm.IsVisible = true);

                // Try to create instance and check if exact alarms can be scheduled
                var alarm = App.ServiceProvider?.GetService(alarmType);
                if (alarm == null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        LblExactAlarmStatus.Text = "Not Available";
                        LblExactAlarmStatus.TextColor = Colors.Gray;
                    });
                    return;
                }

                var canSchedule = alarmType.GetMethod("CanScheduleExactAlarms", BindingFlags.Public | BindingFlags.Instance);
                if (canSchedule != null)
                {
                    var result = canSchedule.Invoke(alarm, null) as bool? ?? false;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (result)
                        {
                            LblExactAlarmStatus.Text = "Enabled ✓";
                            LblExactAlarmStatus.TextColor = Colors.Green;
                            BtnRequestExactAlarm.IsVisible = false;
                        }
                        else
                        {
                            LblExactAlarmStatus.Text = "Not Granted";
                            LblExactAlarmStatus.TextColor = Colors.Orange;
                            BtnRequestExactAlarm.IsVisible = true;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LblExactAlarmStatus.Text = "Error";
                    LblExactAlarmStatus.TextColor = Colors.Red;
                    System.Diagnostics.Debug.WriteLine($"SettingsPage: CheckExactAlarmStatusAsync failed: {ex}");
                });
            }
        }

        private async Task CheckLastSyncAsync()
        {
            try
            {
                if (_cacheService != null)
                {
                    var lastSyncStr = await _cacheService.GetAsync("last_sync_utc").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, out var lastSync))
                    {
                        var localTime = lastSync.ToLocalTime();
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            LblLastSync.Text = localTime.ToString("g");
                            LblLastSync.TextColor = Colors.Green;
                        });
                        return;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LblLastSync.Text = "Never";
                    LblLastSync.TextColor = Colors.Orange;
                });
            }
            catch
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LblLastSync.Text = "Unknown";
                    LblLastSync.TextColor = Colors.Gray;
                });
            }
        }

        private async Task LoadDiagnosticsAsync()
        {
            try
            {
                if (_notificationScheduler != null)
                {
                    var diag = await _notificationScheduler.GetPlatformDiagnosticsAsync().ConfigureAwait(false);
                    var diagStr = diag.Count > 0
                        ? string.Join("\n", diag.Select(kv => $"{kv.Key}: {kv.Value}"))
                        : "No diagnostics available";

                    MainThread.BeginInvokeOnMainThread(() => LblDiagnostics.Text = diagStr);
                }
            }
            catch
            {
                MainThread.BeginInvokeOnMainThread(() => LblDiagnostics.Text = "Diagnostics unavailable");
            }
        }

        private async Task RequestNotificationPermissionAsync()
        {
            try
            {
                // Try to use the platform-specific helper on Android
                var permType = Type.GetType("BloodThinnerTracker.Mobile.Platforms.Android.NotificationPermissionHelper");
                if (permType != null)
                {
                    var reqMethod = permType.GetMethod("RequestPostNotificationsIfNeeded", BindingFlags.Public | BindingFlags.Static);
                    reqMethod?.Invoke(null, null);
                }

                // Refresh status after request
                await Task.Delay(500);
                await CheckNotificationStatusAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to request notification permission: {ex.Message}", "OK");
            }
        }

        private void OpenNotificationSettings()
        {
            try
            {
                AppInfo.ShowSettingsUI();
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlertAsync("Error", $"Could not open settings: {ex.Message}", "OK");
                });
            }
        }

        private void RequestExactAlarmPermission()
        {
            try
            {
#if ANDROID
                // Open the exact alarm settings page
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                intent.SetFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
#else
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlertAsync("Not Supported", "Exact alarm permission is only available on Android.", "OK");
                });
#endif
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlertAsync("Error", $"Could not open exact alarm settings: {ex.Message}", "OK");
                });
            }
        }

        private void OpenBatterySettings()
        {
            try
            {
#if ANDROID
                // Open battery optimization settings
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
                intent.SetFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
#else
                // On other platforms, just open app settings
                AppInfo.ShowSettingsUI();
#endif
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlertAsync("Error", $"Could not open battery settings: {ex.Message}", "OK");
                });
            }
        }

        private async Task SyncNowAsync()
        {
            try
            {
                BtnSyncNow.IsEnabled = false;
                BtnSyncNow.Text = "Syncing...";

                if (_syncWorker != null)
                {
                    await _syncWorker.SyncOnceAsync().ConfigureAwait(false);
                }

                await CheckLastSyncAsync();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    BtnSyncNow.IsEnabled = true;
                    BtnSyncNow.Text = "Sync Now";
                    await DisplayAlertAsync("Success", "Sync completed successfully.", "OK");
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    BtnSyncNow.IsEnabled = true;
                    BtnSyncNow.Text = "Sync Now";
                    await DisplayAlertAsync("Error", $"Sync failed: {ex.Message}", "OK");
                });
            }
        }

        private async Task ViewScheduledRemindersAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//debug/reminders");
            }
            catch
            {
                // If Shell navigation fails, try pushing the page directly
                try
                {
                    var debugPage = App.ServiceProvider?.GetService<Views.DebugScheduledRemindersPage>();
                    if (debugPage != null)
                    {
                        await Navigation.PushAsync(debugPage);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Could not navigate to scheduled reminders: {ex.Message}", "OK");
                }
            }
        }
    }
}
