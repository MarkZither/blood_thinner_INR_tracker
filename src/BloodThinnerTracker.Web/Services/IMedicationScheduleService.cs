namespace BloodThinnerTracker.Web.Services;

using BloodThinnerTracker.Shared.Models;

/// <summary>
/// Service interface for retrieving medication dosage schedules.
/// Provides day-by-day dosage plans based on active patterns.
/// </summary>
public interface IMedicationScheduleService
{
    /// <summary>
    /// Gets the dosage schedule for a medication over a specified period.
    /// </summary>
    /// <param name="medicationPublicId">Public ID (GUID) of the medication</param>
    /// <param name="startDate">Start date for schedule (default: today)</param>
    /// <param name="days">Number of days to calculate (default: 14, max: 365)</param>
    /// <param name="includePatternChanges">Include pattern transition detection (default: true)</param>
    /// <returns>Day-by-day dosage schedule with summary statistics, or null if not found</returns>
    Task<MedicationScheduleResponse?> GetScheduleAsync(
        Guid medicationPublicId,
        DateTime? startDate = null,
        int days = 14,
        bool includePatternChanges = true);
}
