using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodThinnerTracker.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Service responsible for computing upcoming medication reminders
    /// based on active medications with reminders enabled.
    /// </summary>
    public interface IReminderComputationService
    {
        /// <summary>
        /// Computes upcoming medication reminders for the given time window.
        /// </summary>
        /// <param name="medications">Active medications to compute reminders for.</param>
        /// <param name="windowHours">Number of hours ahead to look for reminders (default 24).</param>
        /// <returns>List of notification requests to schedule.</returns>
        IEnumerable<NotificationRequest> ComputeUpcomingReminders(IEnumerable<Medication> medications, int windowHours = 24);
    }

    /// <summary>
    /// Default implementation of IReminderComputationService.
    /// Computes reminder times based on medication scheduled times and frequency.
    /// </summary>
    public class ReminderComputationService : IReminderComputationService
    {
        private readonly ILogger<ReminderComputationService> _logger;

        public ReminderComputationService(ILogger<ReminderComputationService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ReminderComputationService>.Instance;
        }

        public IEnumerable<NotificationRequest> ComputeUpcomingReminders(IEnumerable<Medication> medications, int windowHours = 24)
        {
            var now = DateTimeOffset.Now;
            var windowEnd = now.AddHours(windowHours);
            var reminders = new List<NotificationRequest>();

            foreach (var med in medications)
            {
                // Skip inactive medications or those with reminders disabled
                if (!med.IsActive || !med.RemindersEnabled)
                {
                    continue;
                }

                // Skip medications that haven't started yet or have ended
                if (med.StartDate.Date > now.Date || (med.EndDate.HasValue && med.EndDate.Value.Date < now.Date))
                {
                    continue;
                }

                // Compute reminder times based on scheduled times
                var scheduledTimes = ParseScheduledTimes(med.ScheduledTimes);
                if (!scheduledTimes.Any())
                {
                    // Fall back to computing times based on frequency
                    scheduledTimes = ComputeTimesFromFrequency(med.Frequency);
                }

                foreach (var scheduledTime in scheduledTimes)
                {
                    // Compute actual reminder time (scheduled time - reminder minutes advance)
                    var reminderAdvance = TimeSpan.FromMinutes(med.ReminderMinutes);

                    // Check today and tomorrow for reminders in the window
                    for (int dayOffset = 0; dayOffset <= 1; dayOffset++)
                    {
                        var targetDate = now.Date.AddDays(dayOffset);
                        var scheduledDateTime = new DateTimeOffset(
                            targetDate.Year, targetDate.Month, targetDate.Day,
                            scheduledTime.Hours, scheduledTime.Minutes, 0,
                            now.Offset);

                        // Compute reminder time (when to notify)
                        var reminderTime = scheduledDateTime.Subtract(reminderAdvance);

                        // Only include reminders in the future and within the window
                        if (reminderTime > now && reminderTime <= windowEnd)
                        {
                            // Generate a deterministic ID based on medication + scheduled time
                            // This ensures we don't create duplicate reminders for the same schedule
                            var reminderId = GenerateReminderId(med.PublicId, scheduledDateTime);

                            var title = $"Medication Reminder: {med.Name}";
                            var body = $"Time to take {med.Dosage}{med.DosageUnit} of {med.Name}";

                            if (!string.IsNullOrEmpty(med.Instructions))
                            {
                                body += $"\n{med.Instructions}";
                            }

                            reminders.Add(new NotificationRequest(
                                reminderId,
                                reminderTime,
                                title,
                                body,
                                new Dictionary<string, string>
                                {
                                    ["medicationId"] = med.PublicId.ToString(),
                                    ["scheduledTime"] = scheduledDateTime.ToString("o")
                                }
                            ));

                            _logger.LogDebug("Computed reminder for {MedicationName} at {ReminderTime} (scheduled dose at {ScheduledTime})",
                                med.Name, reminderTime, scheduledDateTime);
                        }
                    }
                }
            }

            return reminders;
        }

        /// <summary>
        /// Parses the ScheduledTimes JSON array (e.g., ["08:00", "20:00"]) into TimeSpan values.
        /// </summary>
        private IEnumerable<TimeSpan> ParseScheduledTimes(string? scheduledTimesJson)
        {
            if (string.IsNullOrWhiteSpace(scheduledTimesJson))
            {
                return Enumerable.Empty<TimeSpan>();
            }

            try
            {
                var times = System.Text.Json.JsonSerializer.Deserialize<string[]>(scheduledTimesJson);
                if (times == null || times.Length == 0)
                {
                    return Enumerable.Empty<TimeSpan>();
                }

                var result = new List<TimeSpan>();
                foreach (var t in times)
                {
                    if (TimeSpan.TryParse(t, out var ts))
                    {
                        result.Add(ts);
                    }
                    else if (DateTime.TryParse(t, out var dt))
                    {
                        result.Add(dt.TimeOfDay);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ScheduledTimes JSON: {ScheduledTimes}", scheduledTimesJson);
                return Enumerable.Empty<TimeSpan>();
            }
        }

        /// <summary>
        /// Computes default scheduled times based on medication frequency.
        /// </summary>
        private IEnumerable<TimeSpan> ComputeTimesFromFrequency(MedicationFrequency frequency)
        {
            return frequency switch
            {
                MedicationFrequency.OnceDaily => new[] { new TimeSpan(8, 0, 0) }, // 8 AM
                MedicationFrequency.TwiceDaily => new[] { new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0) }, // 8 AM, 8 PM
                MedicationFrequency.ThreeTimesDaily => new[] { new TimeSpan(8, 0, 0), new TimeSpan(14, 0, 0), new TimeSpan(20, 0, 0) }, // 8 AM, 2 PM, 8 PM
                MedicationFrequency.FourTimesDaily => new[] { new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), new TimeSpan(16, 0, 0), new TimeSpan(20, 0, 0) }, // 8 AM, 12 PM, 4 PM, 8 PM
                MedicationFrequency.EveryOtherDay => new[] { new TimeSpan(8, 0, 0) }, // 8 AM (caller handles day logic)
                MedicationFrequency.Weekly => new[] { new TimeSpan(8, 0, 0) }, // 8 AM (caller handles day logic)
                MedicationFrequency.AsNeeded => Enumerable.Empty<TimeSpan>(), // No scheduled reminders for PRN
                MedicationFrequency.Custom => Enumerable.Empty<TimeSpan>(), // Requires ScheduledTimes to be set
                _ => new[] { new TimeSpan(8, 0, 0) } // Default to 8 AM
            };
        }

        /// <summary>
        /// Generates a deterministic reminder ID based on medication and scheduled time.
        /// This ensures that the same reminder is not scheduled twice.
        /// </summary>
        private Guid GenerateReminderId(Guid medicationId, DateTimeOffset scheduledTime)
        {
            // Create a deterministic GUID by hashing medication ID + scheduled time
            var input = $"{medicationId}:{scheduledTime:yyyyMMddHHmm}";
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }
}
