// BloodThinnerTracker.Api - Background Service for Medical Reminders
// Licensed under MIT License. See LICENSE file in the project root.

using Microsoft.EntityFrameworkCore;
using BloodThinnerTracker.Api.Data;
using BloodThinnerTracker.Api.Hubs;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Services;

/// <summary>
/// Background service for managing medical reminders and notifications.
/// 
/// ⚠️ MEDICAL REMINDER SERVICE:
/// This service monitors medication schedules and INR test schedules to send
/// timely reminders to users for medication adherence and test compliance.
/// 
/// IMPORTANT MEDICAL DISCLAIMER:
/// This service provides supplementary reminders only. Users should not rely
/// solely on automated reminders for critical medical decisions. Always
/// maintain backup reminder systems and consult healthcare providers.
/// </summary>
public class MedicalReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicalReminderService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

    /// <summary>
    /// Initializes a new instance of the <see cref="MedicalReminderService"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="logger">Logger for reminder operations.</param>
    public MedicalReminderService(
        IServiceProvider serviceProvider,
        ILogger<MedicalReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background reminder service.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for stopping the service.</param>
    /// <returns>A task that represents the asynchronous execution.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Medical Reminder Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMedicationReminders(stoppingToken);
                await ProcessINRReminders(stoppingToken);
                await ProcessCriticalAlerts(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing medical reminders");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Medical Reminder Service stopped");
    }

    /// <summary>
    /// Processes medication reminders for all active medications.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous processing.</returns>
    private async Task ProcessMedicationReminders(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IMedicalNotificationService>();

        var currentTime = DateTime.UtcNow;
        var reminderWindow = TimeSpan.FromMinutes(30); // 30-minute reminder window

        try
        {
            // Find medications that need reminders
            var medicationsNeedingReminders = await dbContext.Medications
                .Where(m => m.IsActive && 
                           m.RemindersEnabled && 
                           !m.IsDeleted &&
                           (m.EndDate == null || m.EndDate > currentTime))
                .Include(m => m.User)
                .ToListAsync(cancellationToken);

            foreach (var medication in medicationsNeedingReminders)
            {
                var scheduledTimes = GetScheduledTimes(medication);
                
                foreach (var scheduledTime in scheduledTimes)
                {
                    var reminderTime = scheduledTime.AddMinutes(-medication.ReminderMinutes);
                    
                    // Check if we should send a reminder
                    if (ShouldSendReminder(currentTime, reminderTime, reminderWindow))
                    {
                        await SendMedicationReminder(medication, scheduledTime, notificationService);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing medication reminders");
        }
    }

    /// <summary>
    /// Processes INR test reminders for all active schedules.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous processing.</returns>
    private async Task ProcessINRReminders(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IMedicalNotificationService>();

        var currentDate = DateTime.UtcNow.Date;

        try
        {
            // Find INR schedules that need reminders
            var schedulesNeedingReminders = await dbContext.INRSchedules
                .Where(s => s.Status == INRScheduleStatus.Active &&
                           s.RemindersEnabled &&
                           !s.IsDeleted &&
                           s.CompletedDate == null &&
                           (s.EndDate == null || s.EndDate > currentDate))
                .Include(s => s.User)
                .ToListAsync(cancellationToken);

            foreach (var schedule in schedulesNeedingReminders)
            {
                var reminderDate = schedule.ScheduledDate.AddDays(-schedule.ReminderDays);
                
                // Check if we should send a reminder (within the reminder window)
                if (currentDate >= reminderDate && currentDate <= schedule.ScheduledDate)
                {
                    await SendINRTestReminder(schedule, notificationService);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing INR test reminders");
        }
    }

    /// <summary>
    /// Processes critical medical alerts for dangerous conditions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous processing.</returns>
    private async Task ProcessCriticalAlerts(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IMedicalNotificationService>();

        var currentTime = DateTime.UtcNow;
        var criticalWindow = TimeSpan.FromHours(24); // 24-hour window for critical checks

        try
        {
            // Check for missed medications (high priority)
            await CheckMissedMedications(dbContext, notificationService, currentTime, criticalWindow);

            // Check for overdue INR tests
            await CheckOverdueINRTests(dbContext, notificationService, currentTime);

            // Check for dangerous INR values
            await CheckDangerousINRValues(dbContext, notificationService, currentTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing critical medical alerts");
        }
    }

    /// <summary>
    /// Gets the scheduled times for a medication based on its frequency.
    /// </summary>
    /// <param name="medication">Medication to get schedule for.</param>
    /// <returns>List of scheduled times for today.</returns>
    private List<DateTime> GetScheduledTimes(Medication medication)
    {
        var scheduledTimes = new List<DateTime>();
        var today = DateTime.UtcNow.Date;

        // Parse scheduled times from medication configuration
        if (!string.IsNullOrEmpty(medication.ScheduledTimes))
        {
            try
            {
                var times = System.Text.Json.JsonSerializer.Deserialize<List<TimeSpan>>(medication.ScheduledTimes);
                if (times != null)
                {
                    scheduledTimes.AddRange(times.Select(time => today.Add(time)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse scheduled times for medication {MedicationId}", medication.Id);
            }
        }

        // Fallback to frequency-based scheduling if no specific times
        if (!scheduledTimes.Any())
        {
            scheduledTimes = GenerateScheduleFromFrequency(medication, today);
        }

        return scheduledTimes.Where(time => time >= medication.StartDate).ToList();
    }

    /// <summary>
    /// Generates a schedule based on medication frequency.
    /// </summary>
    /// <param name="medication">Medication to generate schedule for.</param>
    /// <param name="date">Date to generate schedule for.</param>
    /// <returns>List of scheduled times.</returns>
    private List<DateTime> GenerateScheduleFromFrequency(Medication medication, DateTime date)
    {
        var times = new List<DateTime>();

        switch (medication.Frequency)
        {
            case MedicationFrequency.OnceDaily:
                times.Add(date.AddHours(8)); // 8 AM
                break;
            case MedicationFrequency.TwiceDaily:
                times.Add(date.AddHours(8));  // 8 AM
                times.Add(date.AddHours(20)); // 8 PM
                break;
            case MedicationFrequency.ThreeTimesDaily:
                times.Add(date.AddHours(8));  // 8 AM
                times.Add(date.AddHours(14)); // 2 PM
                times.Add(date.AddHours(20)); // 8 PM
                break;
            case MedicationFrequency.FourTimesDaily:
                times.Add(date.AddHours(6));  // 6 AM
                times.Add(date.AddHours(12)); // 12 PM
                times.Add(date.AddHours(18)); // 6 PM
                times.Add(date.AddHours(24)); // 12 AM (next day)
                break;
            case MedicationFrequency.AsNeeded:
                // No automatic reminders for as-needed medications
                break;
        }

        return times;
    }

    /// <summary>
    /// Determines if a reminder should be sent based on timing.
    /// </summary>
    /// <param name="currentTime">Current time.</param>
    /// <param name="reminderTime">Time when reminder should be sent.</param>
    /// <param name="window">Acceptable window for sending reminder.</param>
    /// <returns>True if reminder should be sent.</returns>
    private bool ShouldSendReminder(DateTime currentTime, DateTime reminderTime, TimeSpan window)
    {
        return currentTime >= reminderTime && currentTime <= reminderTime.Add(window);
    }

    /// <summary>
    /// Sends a medication reminder notification.
    /// </summary>
    /// <param name="medication">Medication requiring reminder.</param>
    /// <param name="scheduledTime">Scheduled time for medication.</param>
    /// <param name="notificationService">Notification service.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    private async Task SendMedicationReminder(
        Medication medication, 
        DateTime scheduledTime, 
        IMedicalNotificationService notificationService)
    {
        var reminder = new
        {
            Id = Guid.NewGuid().ToString(),
            Type = "MedicationReminder",
            MedicationId = medication.Id,
            MedicationName = medication.Name,
            Dosage = medication.Dosage,
            DosageUnit = medication.DosageUnit,
            ScheduledTime = scheduledTime,
            ReminderTime = DateTime.UtcNow,
            Priority = "Normal",
            Message = $"Time to take your {medication.Name} ({medication.Dosage} {medication.DosageUnit})",
            Instructions = medication.Instructions,
            SafetyNote = "⚠️ Take exactly as prescribed. Contact your healthcare provider if you have concerns."
        };

        await notificationService.SendMedicationReminderAsync(new List<string> { medication.UserId }, reminder);
        
        _logger.LogInformation("Sent medication reminder for {MedicationName} to user {UserId}", 
            medication.Name, medication.UserId);
    }

    /// <summary>
    /// Sends an INR test reminder notification.
    /// </summary>
    /// <param name="schedule">INR schedule requiring reminder.</param>
    /// <param name="notificationService">Notification service.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    private async Task SendINRTestReminder(
        INRSchedule schedule, 
        IMedicalNotificationService notificationService)
    {
        var daysUntilTest = (schedule.ScheduledDate - DateTime.UtcNow.Date).Days;
        
        var reminder = new
        {
            Id = Guid.NewGuid().ToString(),
            Type = "INRTestReminder",
            ScheduleId = schedule.Id,
            ScheduledDate = schedule.ScheduledDate,
            DaysUntilTest = daysUntilTest,
            TargetINRRange = schedule.TargetINRMin.HasValue && schedule.TargetINRMax.HasValue 
                ? $"{schedule.TargetINRMin} - {schedule.TargetINRMax}" 
                : "As prescribed",
            PreferredLaboratory = schedule.PreferredLaboratory,
            Priority = daysUntilTest <= 1 ? "High" : "Normal",
            Message = daysUntilTest == 0 
                ? "Your INR test is scheduled for today"
                : $"Your INR test is scheduled in {daysUntilTest} day(s)",
            Instructions = schedule.TestingInstructions,
            SafetyNote = "⚠️ Regular INR monitoring is essential for safe blood thinner therapy."
        };

        await notificationService.SendINRReminderAsync(new List<string> { schedule.UserId }, reminder);
        
        _logger.LogInformation("Sent INR test reminder for schedule {ScheduleId} to user {UserId}", 
            schedule.Id, schedule.UserId);
    }

    /// <summary>
    /// Checks for missed medications and sends critical alerts.
    /// </summary>
    /// <param name="dbContext">Database context.</param>
    /// <param name="notificationService">Notification service.</param>
    /// <param name="currentTime">Current time.</param>
    /// <param name="window">Time window to check for missed medications.</param>
    /// <returns>A task that represents the asynchronous check operation.</returns>
    private async Task CheckMissedMedications(
        ApplicationDbContext dbContext,
        IMedicalNotificationService notificationService,
        DateTime currentTime,
        TimeSpan window)
    {
        var cutoffTime = currentTime.Subtract(window);

        // Find users who have missed critical medications
        var missedMedications = await dbContext.Medications
            .Where(m => m.IsActive && !m.IsDeleted)
            .Where(m => !dbContext.MedicationLogs
                .Any(log => log.MedicationId == m.Id && 
                           log.ScheduledTime >= cutoffTime &&
                           log.Status == MedicationLogStatus.Taken))
            .ToListAsync();

        foreach (var medication in missedMedications)
        {
            var alert = new
            {
                Id = Guid.NewGuid().ToString(),
                Type = "CriticalMissedMedication",
                MedicationId = medication.Id,
                MedicationName = medication.Name,
                Priority = "Critical",
                Message = $"CRITICAL: You may have missed your {medication.Name}. Please take it now if still within the safe window.",
                SafetyNote = "⚠️ CRITICAL: Contact your healthcare provider immediately if you're unsure about missed doses."
            };

            await notificationService.SendCriticalAlertAsync(new List<string> { medication.UserId }, alert);
        }
    }

    /// <summary>
    /// Checks for overdue INR tests and sends alerts.
    /// </summary>
    /// <param name="dbContext">Database context.</param>
    /// <param name="notificationService">Notification service.</param>
    /// <param name="currentTime">Current time.</param>
    /// <returns>A task that represents the asynchronous check operation.</returns>
    private async Task CheckOverdueINRTests(
        ApplicationDbContext dbContext,
        IMedicalNotificationService notificationService,
        DateTime currentTime)
    {
        var overdueSchedules = await dbContext.INRSchedules
            .Where(s => s.Status == INRScheduleStatus.Active &&
                       s.ScheduledDate < currentTime.Date &&
                       s.CompletedDate == null &&
                       !s.IsDeleted)
            .ToListAsync();

        foreach (var schedule in overdueSchedules)
        {
            var daysOverdue = (currentTime.Date - schedule.ScheduledDate).Days;
            
            var alert = new
            {
                Id = Guid.NewGuid().ToString(),
                Type = "OverdueINRTest",
                ScheduleId = schedule.Id,
                DaysOverdue = daysOverdue,
                Priority = daysOverdue >= 7 ? "Critical" : "High",
                Message = $"Your INR test is {daysOverdue} day(s) overdue. Please schedule it immediately.",
                SafetyNote = "⚠️ Overdue INR tests can compromise medication safety. Contact your healthcare provider."
            };

            await notificationService.SendCriticalAlertAsync(new List<string> { schedule.UserId }, alert);
        }
    }

    /// <summary>
    /// Checks for dangerous INR values and sends critical alerts.
    /// </summary>
    /// <param name="dbContext">Database context.</param>
    /// <param name="notificationService">Notification service.</param>
    /// <param name="currentTime">Current time.</param>
    /// <returns>A task that represents the asynchronous check operation.</returns>
    private async Task CheckDangerousINRValues(
        ApplicationDbContext dbContext,
        IMedicalNotificationService notificationService,
        DateTime currentTime)
    {
        var recentCutoff = currentTime.AddDays(-30); // Check last 30 days
        
        var dangerousINRTests = await dbContext.INRTests
            .Where(t => t.TestDate >= recentCutoff &&
                       (t.INRValue < 0.8m || t.INRValue > 5.0m) && // Dangerous ranges
                       !t.IsDeleted)
            .OrderByDescending(t => t.TestDate)
            .ToListAsync();

        foreach (var test in dangerousINRTests)
        {
            var alert = new
            {
                Id = Guid.NewGuid().ToString(),
                Type = "DangerousINRValue",
                TestId = test.Id,
                INRValue = test.INRValue,
                TestDate = test.TestDate,
                Priority = "Critical",
                Message = test.INRValue < 0.8m 
                    ? "CRITICAL: Your INR is dangerously low. Risk of blood clots."
                    : "CRITICAL: Your INR is dangerously high. Risk of bleeding.",
                SafetyNote = "⚠️ EMERGENCY: Contact your healthcare provider or emergency services immediately."
            };

            await notificationService.SendCriticalAlertAsync(new List<string> { test.UserId }, alert);
        }
    }
}