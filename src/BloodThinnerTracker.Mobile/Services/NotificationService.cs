using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// Service for managing medication reminders and notifications
/// </summary>
public interface INotificationService
{
    Task InitializeAsync();
    Task<bool> RequestPermissionAsync();
    Task ScheduleMedicationReminderAsync(int medicationId, string medicationName, DateTime scheduledTime, bool isRecurring = true);
    Task ScheduleINRReminderAsync(DateTime testDate, string message);
    Task CancelMedicationReminderAsync(int medicationId);
    Task CancelAllRemindersAsync();
    Task ShowImmediateNotificationAsync(string title, string message, NotificationType type = NotificationType.Info);
    Task CheckMissedRemindersAsync();
    event EventHandler<NotificationEventArgs> NotificationReceived;
}

public class NotificationService : INotificationService
{
    private readonly ISecureStorageService _secureStorage;
    private const string NOTIFICATIONS_ENABLED_KEY = "notifications_enabled";
    
    public event EventHandler<NotificationEventArgs> NotificationReceived = delegate { };

    public NotificationService(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Set up notification event handlers
            LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;
            LocalNotificationCenter.Current.NotificationReceived += OnNotificationReceived;
            
            // Request permissions
            await RequestPermissionAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Notification initialization error: {ex.Message}");
        }
    }

    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            var request = new NotificationPermission
            {
                Title = "Medication Reminders",
                Subtitle = "Allow notifications to receive medication and INR test reminders",
                Description = "Blood Thinner Tracker needs permission to send you important medication reminders and INR test notifications."
            };

            var result = await LocalNotificationCenter.Current.RequestNotificationPermission(request);
            await _secureStorage.SetAsync(NOTIFICATIONS_ENABLED_KEY, result.ToString());
            
            return result == NotificationPermission.Granted;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Permission request error: {ex.Message}");
            return false;
        }
    }

    public async Task ScheduleMedicationReminderAsync(int medicationId, string medicationName, DateTime scheduledTime, bool isRecurring = true)
    {
        try
        {
            var notificationId = GenerateMedicationNotificationId(medicationId);
            
            var notification = new NotificationRequest
            {
                NotificationId = notificationId,
                Title = "💊 Medication Reminder",
                Subtitle = $"Time to take {medicationName}",
                Description = $"It's time to take your {medicationName}. Tap to log this dose.",
                BadgeNumber = 1,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = scheduledTime,
                    RepeatType = isRecurring ? NotificationRepeat.Daily : NotificationRepeat.No
                },
                CategoryType = NotificationCategoryType.Reminder,
                Android = new AndroidOptions
                {
                    ChannelId = "medication_reminders",
                    Priority = AndroidPriority.High,
                    VibrationPattern = new long[] { 0, 250, 250, 250 }
                },
                iOS = new iOSOptions
                {
                    HideForegroundAlert = false,
                    PlaySound = true,
                    SoundName = "medication_sound.wav"
                }
            };

            // Add action buttons
            notification.Android.Actions = new List<AndroidAction>
            {
                new AndroidAction
                {
                    ActionId = "take_now",
                    Title = "Take Now",
                    Icon = "ic_check",
                    LaunchApp = true
                },
                new AndroidAction
                {
                    ActionId = "snooze_15",
                    Title = "Snooze 15min",
                    Icon = "ic_snooze",
                    LaunchApp = false
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
            
            System.Diagnostics.Debug.WriteLine($"Scheduled medication reminder for {medicationName} at {scheduledTime}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scheduling medication reminder: {ex.Message}");
        }
    }

    public async Task ScheduleINRReminderAsync(DateTime testDate, string message)
    {
        try
        {
            var notificationId = GenerateINRNotificationId(testDate);
            
            var notification = new NotificationRequest
            {
                NotificationId = notificationId,
                Title = "🩸 INR Test Reminder",
                Subtitle = "Time for your INR test",
                Description = message,
                BadgeNumber = 1,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = testDate.AddHours(-2), // Remind 2 hours before
                    RepeatType = NotificationRepeat.No
                },
                CategoryType = NotificationCategoryType.Reminder,
                Android = new AndroidOptions
                {
                    ChannelId = "inr_reminders",
                    Priority = AndroidPriority.High
                },
                iOS = new iOSOptions
                {
                    HideForegroundAlert = false,
                    PlaySound = true
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scheduling INR reminder: {ex.Message}");
        }
    }

    public async Task CancelMedicationReminderAsync(int medicationId)
    {
        try
        {
            var notificationId = GenerateMedicationNotificationId(medicationId);
            await LocalNotificationCenter.Current.Cancel(notificationId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error canceling medication reminder: {ex.Message}");
        }
    }

    public async Task CancelAllRemindersAsync()
    {
        try
        {
            await LocalNotificationCenter.Current.CancelAll();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error canceling all reminders: {ex.Message}");
        }
    }

    public async Task ShowImmediateNotificationAsync(string title, string message, NotificationType type = NotificationType.Info)
    {
        try
        {
            var icon = type switch
            {
                NotificationType.Success => "✅",
                NotificationType.Warning => "⚠️",
                NotificationType.Error => "❌",
                NotificationType.Critical => "🚨",
                _ => "ℹ️"
            };

            var notification = new NotificationRequest
            {
                NotificationId = Random.Shared.Next(1000, 9999),
                Title = $"{icon} {title}",
                Description = message,
                BadgeNumber = 1,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddSeconds(1)
                },
                Android = new AndroidOptions
                {
                    ChannelId = type == NotificationType.Critical ? "critical_alerts" : "general_notifications",
                    Priority = type == NotificationType.Critical ? AndroidPriority.Max : AndroidPriority.Default
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing immediate notification: {ex.Message}");
        }
    }

    public async Task CheckMissedRemindersAsync()
    {
        try
        {
            // This would check for any missed medication times and show appropriate notifications
            var now = DateTime.Now;
            var cutoffTime = now.AddHours(-2); // Check for reminders in the last 2 hours

            // Implementation would query pending notifications and check if any were missed
            // For now, just log that we're checking
            System.Diagnostics.Debug.WriteLine($"Checking for missed reminders since {cutoffTime}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking missed reminders: {ex.Message}");
        }
    }

    private void OnNotificationActionTapped(NotificationActionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Notification action tapped: {e.ActionId}");
        
        // Handle different actions
        switch (e.ActionId)
        {
            case "take_now":
                // Navigate to medication logging
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync("//medication");
                });
                break;
                
            case "snooze_15":
                // Reschedule notification for 15 minutes later
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Implementation would reschedule the notification
                });
                break;
        }
    }

    private void OnNotificationReceived(NotificationEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Notification received: {e.Title}");
        NotificationReceived?.Invoke(this, e);
    }

    private int GenerateMedicationNotificationId(int medicationId)
    {
        return 1000 + medicationId; // Offset to avoid conflicts
    }

    private int GenerateINRNotificationId(DateTime testDate)
    {
        return 2000 + testDate.DayOfYear; // Offset for INR notifications
    }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Critical
}