using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Services
{
    public record NotificationRequest
    (
        Guid Id,
        DateTimeOffset TriggerTime,
        string Title,
        string Body,
        IDictionary<string, string>? Data = null
    );

    public record NotificationInfo
    (
        Guid Id,
        DateTimeOffset TriggerTime,
        string Title,
        string Body,
        bool IsScheduled,
        int? RequestCode = null,
        string? AlarmType = null
    );

    /// <summary>
    /// Platform-agnostic notification scheduler interface.
    /// Implementations should schedule local notifications and provide query/cancel semantics.
    /// </summary>
    public interface INotificationScheduler
    {
        Task ScheduleAsync(NotificationRequest request);
        Task CancelAsync(Guid reminderId);
        Task<NotificationInfo?> GetScheduledAsync(Guid reminderId);
        Task<IEnumerable<NotificationInfo>> GetAllScheduledAsync();
    }
}
