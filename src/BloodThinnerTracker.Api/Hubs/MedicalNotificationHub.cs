// BloodThinnerTracker.Api - SignalR Hub for Medical Notifications
// Licensed under MIT License. See LICENSE file in the project root.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BloodThinnerTracker.Api.Hubs;

/// <summary>
/// SignalR hub for real-time medical notifications and cross-device synchronization.
/// 
/// ⚠️ MEDICAL NOTIFICATION HUB:
/// This hub provides real-time communication for medication reminders, INR alerts,
/// and synchronized updates across multiple devices for medical safety.
/// 
/// IMPORTANT MEDICAL DISCLAIMER:
/// Real-time notifications are supplementary to proper medical care. Users should
/// never rely solely on electronic reminders for critical medication timing.
/// Always consult healthcare providers for medical decisions.
/// </summary>
[Authorize]
public class MedicalNotificationHub : Hub
{
    private readonly ILogger<MedicalNotificationHub> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MedicalNotificationHub"/> class.
    /// </summary>
    /// <param name="logger">Logger for hub operations.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    public MedicalNotificationHub(
        ILogger<MedicalNotificationHub> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// Automatically joins the user to their personal notification group.
    /// </summary>
    /// <returns>A task that represents the asynchronous connect operation.</returns>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId != null)
        {
            // Join user-specific group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Join device-specific group for cross-device sync
            var deviceId = Context.GetHttpContext()?.Request.Headers["X-Device-ID"].FirstOrDefault();
            if (!string.IsNullOrEmpty(deviceId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
            }

            _logger.LogInformation("Medical notification client connected: User {UserId}, Connection {ConnectionId}", 
                userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">Exception that caused the disconnection, if any.</param>
    /// <returns>A task that represents the asynchronous disconnect operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId != null)
        {
            _logger.LogInformation("Medical notification client disconnected: User {UserId}, Connection {ConnectionId}", 
                userId, Context.ConnectionId);
        }

        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes client to medication reminder notifications for specific medications.
    /// </summary>
    /// <param name="medicationIds">List of medication IDs to subscribe to.</param>
    /// <returns>A task that represents the asynchronous subscription operation.</returns>
    public async Task SubscribeToMedicationReminders(List<string> medicationIds)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        foreach (var medicationId in medicationIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"medication_{medicationId}");
        }

        _logger.LogDebug("User {UserId} subscribed to medication reminders: {MedicationIds}", 
            userId, string.Join(", ", medicationIds));
    }

    /// <summary>
    /// Unsubscribes client from medication reminder notifications.
    /// </summary>
    /// <param name="medicationIds">List of medication IDs to unsubscribe from.</param>
    /// <returns>A task that represents the asynchronous unsubscription operation.</returns>
    public async Task UnsubscribeFromMedicationReminders(List<string> medicationIds)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        foreach (var medicationId in medicationIds)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"medication_{medicationId}");
        }

        _logger.LogDebug("User {UserId} unsubscribed from medication reminders: {MedicationIds}", 
            userId, string.Join(", ", medicationIds));
    }

    /// <summary>
    /// Subscribes client to INR test reminder notifications.
    /// </summary>
    /// <returns>A task that represents the asynchronous subscription operation.</returns>
    public async Task SubscribeToINRReminders()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"inr_reminders_{userId}");
        
        _logger.LogDebug("User {UserId} subscribed to INR reminders", userId);
    }

    /// <summary>
    /// Unsubscribes client from INR test reminder notifications.
    /// </summary>
    /// <returns>A task that represents the asynchronous unsubscription operation.</returns>
    public async Task UnsubscribeFromINRReminders()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"inr_reminders_{userId}");
        
        _logger.LogDebug("User {UserId} unsubscribed from INR reminders", userId);
    }

    /// <summary>
    /// Updates client presence status for cross-device awareness.
    /// </summary>
    /// <param name="status">Current status (online, away, busy, etc.).</param>
    /// <param name="deviceType">Type of device (mobile, web, desktop).</param>
    /// <returns>A task that represents the asynchronous status update operation.</returns>
    public async Task UpdatePresenceStatus(string status, string deviceType)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        // Broadcast status to all user's devices except the sender
        await Clients.GroupExcept($"user_{userId}", Context.ConnectionId)
            .SendAsync("PresenceStatusUpdated", new
            {
                UserId = userId,
                Status = status,
                DeviceType = deviceType,
                Timestamp = DateTime.UtcNow,
                ConnectionId = Context.ConnectionId
            });

        _logger.LogDebug("User {UserId} updated presence status: {Status} on {DeviceType}", 
            userId, status, deviceType);
    }

    /// <summary>
    /// Acknowledges receipt of a medication reminder to prevent duplicate notifications.
    /// </summary>
    /// <param name="reminderId">ID of the reminder being acknowledged.</param>
    /// <param name="medicationLogId">ID of the medication log entry (if medication was taken).</param>
    /// <returns>A task that represents the asynchronous acknowledgment operation.</returns>
    public async Task AcknowledgeMedicationReminder(string reminderId, string? medicationLogId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        // Broadcast acknowledgment to all user's devices to stop showing the reminder
        await Clients.Group($"user_{userId}")
            .SendAsync("MedicationReminderAcknowledged", new
            {
                ReminderId = reminderId,
                MedicationLogId = medicationLogId,
                AcknowledgedAt = DateTime.UtcNow,
                AcknowledgedBy = userId
            });

        _logger.LogInformation("User {UserId} acknowledged medication reminder {ReminderId}", 
            userId, reminderId);
    }

    /// <summary>
    /// Acknowledges receipt of an INR test reminder.
    /// </summary>
    /// <param name="scheduleId">ID of the INR schedule being acknowledged.</param>
    /// <param name="testId">ID of the INR test (if test was completed).</param>
    /// <returns>A task that represents the asynchronous acknowledgment operation.</returns>
    public async Task AcknowledgeINRReminder(string scheduleId, string? testId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        // Broadcast acknowledgment to all user's devices
        await Clients.Group($"user_{userId}")
            .SendAsync("INRReminderAcknowledged", new
            {
                ScheduleId = scheduleId,
                TestId = testId,
                AcknowledgedAt = DateTime.UtcNow,
                AcknowledgedBy = userId
            });

        _logger.LogInformation("User {UserId} acknowledged INR reminder for schedule {ScheduleId}", 
            userId, scheduleId);
    }

    /// <summary>
    /// Requests synchronization of user data across all connected devices.
    /// </summary>
    /// <param name="dataTypes">Types of data to synchronize (medications, tests, schedules).</param>
    /// <returns>A task that represents the asynchronous sync operation.</returns>
    public async Task RequestDataSync(List<string> dataTypes)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        // Notify all user's devices (except sender) to sync data
        await Clients.GroupExcept($"user_{userId}", Context.ConnectionId)
            .SendAsync("DataSyncRequested", new
            {
                DataTypes = dataTypes,
                RequestedBy = Context.ConnectionId,
                Timestamp = DateTime.UtcNow
            });

        _logger.LogDebug("User {UserId} requested data sync for: {DataTypes}", 
            userId, string.Join(", ", dataTypes));
    }

    /// <summary>
    /// Gets the current user ID from the connection context.
    /// </summary>
    /// <returns>User ID if authenticated, null otherwise.</returns>
    private string? GetCurrentUserId()
    {
        return Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
}

/// <summary>
/// Service for sending medical notifications through SignalR.
/// </summary>
public interface IMedicalNotificationService
{
    /// <summary>
    /// Sends a medication reminder to specific users.
    /// </summary>
    /// <param name="userIds">List of user IDs to notify.</param>
    /// <param name="reminder">Medication reminder details.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    Task SendMedicationReminderAsync(List<string> userIds, object reminder);

    /// <summary>
    /// Sends an INR test reminder to specific users.
    /// </summary>
    /// <param name="userIds">List of user IDs to notify.</param>
    /// <param name="reminder">INR test reminder details.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    Task SendINRReminderAsync(List<string> userIds, object reminder);

    /// <summary>
    /// Sends a critical medical alert to specific users.
    /// </summary>
    /// <param name="userIds">List of user IDs to notify.</param>
    /// <param name="alert">Critical alert details.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    Task SendCriticalAlertAsync(List<string> userIds, object alert);

    /// <summary>
    /// Notifies users about data synchronization updates.
    /// </summary>
    /// <param name="userIds">List of user IDs to notify.</param>
    /// <param name="syncData">Synchronization data.</param>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    Task NotifyDataSyncAsync(List<string> userIds, object syncData);
}

/// <summary>
/// Default implementation of the medical notification service.
/// </summary>
public class MedicalNotificationService : IMedicalNotificationService
{
    private readonly IHubContext<MedicalNotificationHub> _hubContext;
    private readonly ILogger<MedicalNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MedicalNotificationService"/> class.
    /// </summary>
    /// <param name="hubContext">SignalR hub context.</param>
    /// <param name="logger">Logger for notification operations.</param>
    public MedicalNotificationService(
        IHubContext<MedicalNotificationHub> hubContext,
        ILogger<MedicalNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendMedicationReminderAsync(List<string> userIds, object reminder)
    {
        try
        {
            var tasks = userIds.Select(userId => 
                _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("MedicationReminderReceived", reminder));

            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Sent medication reminder to {UserCount} users", userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send medication reminder to users: {UserIds}", 
                string.Join(", ", userIds));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendINRReminderAsync(List<string> userIds, object reminder)
    {
        try
        {
            var tasks = userIds.Select(userId => 
                _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("INRReminderReceived", reminder));

            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Sent INR reminder to {UserCount} users", userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send INR reminder to users: {UserIds}", 
                string.Join(", ", userIds));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendCriticalAlertAsync(List<string> userIds, object alert)
    {
        try
        {
            var tasks = userIds.Select(userId => 
                _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("CriticalAlertReceived", alert));

            await Task.WhenAll(tasks);
            
            _logger.LogWarning("Sent critical medical alert to {UserCount} users", userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send critical alert to users: {UserIds}", 
                string.Join(", ", userIds));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task NotifyDataSyncAsync(List<string> userIds, object syncData)
    {
        try
        {
            var tasks = userIds.Select(userId => 
                _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("DataSyncNotification", syncData));

            await Task.WhenAll(tasks);
            
            _logger.LogDebug("Sent data sync notification to {UserCount} users", userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data sync notification to users: {UserIds}", 
                string.Join(", ", userIds));
            throw;
        }
    }
}