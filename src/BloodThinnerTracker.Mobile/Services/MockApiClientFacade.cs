using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiSdk.Models;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// Mock implementation of IApiClientFacade for offline development and testing.
/// Returns static sample data without making any remote API calls.
/// Includes medications with reminders scheduled in the next 15 minutes for testing.
/// </summary>
public class MockApiClientFacade : IApiClientFacade
{
    private readonly ILogger<MockApiClientFacade>? _logger;

    public MockApiClientFacade(ILogger<MockApiClientFacade>? logger = null)
    {
        _logger = logger;
    }

    public async Task<List<MedicationResponse>?> GetMedicationsAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("MockApiClientFacade: returning sample medications with upcoming reminders");
        await Task.Delay(100, cancellationToken); // Simulate network latency

        var now = DateTime.Now;
        
        // Create scheduled times for the next 15 minutes (at 3, 6, 9, 12, 15 minute marks)
        var upcomingTimes = new List<string>();
        for (int i = 3; i <= 15; i += 3)
        {
            var reminderTime = now.AddMinutes(i);
            upcomingTimes.Add(reminderTime.ToString("HH:mm"));
        }

        return
        [
            new MedicationResponse
            {
                PublicId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Warfarin",
                GenericName = "Warfarin Sodium",
                BrandName = "Coumadin",
                Dosage = 5.0m,
                DosageUnit = "mg",
                Form = "Tablet",
                Color = "Pink",
                IsBloodThinner = true,
                InrTargetMin = 2.0m,
                InrTargetMax = 3.0m,
                Frequency = 1, // Daily
                ScheduledTimes = JsonSerializer.Serialize(new[] { upcomingTimes[0], upcomingTimes[2] }), // 3 min and 9 min
                IsActive = true,
                StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
                RemindersEnabled = true,
                ReminderMinutes = 0,
                Instructions = "Take with food. Avoid vitamin K-rich foods.",
                Notes = "Mock medication for testing reminders"
            },
            new MedicationResponse
            {
                PublicId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Apixaban",
                GenericName = "Apixaban",
                BrandName = "Eliquis",
                Dosage = 2.5m,
                DosageUnit = "mg",
                Form = "Tablet",
                Color = "Yellow",
                IsBloodThinner = true,
                Frequency = 2, // TwiceDaily
                ScheduledTimes = JsonSerializer.Serialize(new[] { upcomingTimes[1] }), // 6 min
                IsActive = true,
                StartDate = DateTimeOffset.UtcNow.AddDays(-7),
                RemindersEnabled = true,
                ReminderMinutes = 0,
                Instructions = "Take with or without food.",
                Notes = "Mock medication for testing reminders"
            },
            new MedicationResponse
            {
                PublicId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Aspirin",
                GenericName = "Acetylsalicylic Acid",
                BrandName = "Bayer",
                Dosage = 81.0m,
                DosageUnit = "mg",
                Form = "Tablet",
                Color = "Orange",
                IsBloodThinner = false,
                Frequency = 1, // Daily
                ScheduledTimes = JsonSerializer.Serialize(new[] { upcomingTimes[3] }), // 12 min
                IsActive = true,
                StartDate = DateTimeOffset.UtcNow.AddMonths(-2),
                RemindersEnabled = true,
                ReminderMinutes = 0,
                Instructions = "Take with food to reduce stomach upset.",
                Notes = "Low-dose aspirin for cardiovascular protection"
            },
            new MedicationResponse
            {
                PublicId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Rivaroxaban",
                GenericName = "Rivaroxaban",
                BrandName = "Xarelto",
                Dosage = 20.0m,
                DosageUnit = "mg",
                Form = "Tablet",
                Color = "Red",
                IsBloodThinner = true,
                Frequency = 1, // Daily
                ScheduledTimes = JsonSerializer.Serialize(new[] { upcomingTimes[4] }), // 15 min
                IsActive = true,
                StartDate = DateTimeOffset.UtcNow.AddDays(-14),
                RemindersEnabled = true,
                ReminderMinutes = 0,
                Instructions = "Take with evening meal.",
                Notes = "Direct oral anticoagulant (DOAC)"
            }
        ];
    }

    public async Task<List<MedicationLogResponse>?> GetMedicationLogsAsync(Guid medicationPublicId, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("MockApiClientFacade: returning sample medication logs for {MedicationId}", medicationPublicId);
        await Task.Delay(100, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        return
        [
            new MedicationLogResponse
            {
                PublicId = Guid.NewGuid(),
                MedicationPublicId = medicationPublicId,
                ScheduledTime = now.AddDays(-1),
                ActualTime = now.AddDays(-1),
                ActualDosage = 4.0m,
                ExpectedDosage = 4.0m,
                PatternDayNumber = 1,
                Notes = "Taken on time"
            },
            new MedicationLogResponse
            {
                PublicId = Guid.NewGuid(),
                MedicationPublicId = medicationPublicId,
                ScheduledTime = now.AddDays(-2),
                ActualTime = now.AddDays(-2),
                ActualDosage = 4.0m,
                ExpectedDosage = 4.0m,
                PatternDayNumber = 2,
                Notes = null
            },
            new MedicationLogResponse
            {
                PublicId = Guid.NewGuid(),
                MedicationPublicId = medicationPublicId,
                ScheduledTime = now.AddDays(-3),
                ActualTime = now.AddDays(-3),
                ActualDosage = 3.0m,
                ExpectedDosage = 4.0m,
                PatternDayNumber = 3,
                Notes = "Reduced dose per doctor"
            }
        ];
    }

    public async Task<List<INRTestResponse>?> GetRecentInrTestsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("MockApiClientFacade: returning sample INR tests (take={Take})", take);
        await Task.Delay(100, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var results = new List<INRTestResponse>();

        var sampleValues = new[] { 2.7m, 2.9m, 3.1m, 2.6m, 2.8m, 2.5m, 3.0m, 2.4m, 2.9m, 2.7m };

        for (int i = 0; i < Math.Min(take, sampleValues.Length); i++)
        {
            results.Add(new INRTestResponse
            {
                PublicId = Guid.NewGuid(),
                InrValue = sampleValues[i],
                TestDate = now.AddDays(-i * 7), // Weekly tests
                Notes = i == 0 ? "Most recent test" : null,
                ReviewedByProvider = i > 0 // Mark older tests as reviewed
            });
        }

        return results;
    }
}
