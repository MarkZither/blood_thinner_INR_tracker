/*
 * BloodThinnerTracker.Web - Medication Log Service Interface
 * Licensed under MIT License. See LICENSE file in the project root.
 *
 * Service interface for medication dose logging and adherence tracking operations.
 * Provides methods for recording when medications are taken and viewing medication history.
 */

using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Web.Services;

/// <summary>
/// Service interface for medication dose logging and adherence tracking.
/// Handles recording when medications are taken and viewing medication history.
/// </summary>
public interface IMedicationLogService
{
    /// <summary>
    /// Gets medication logs for a specific medication.
    /// </summary>
    /// <param name="medicationId">Medication ID.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>List of medication logs.</returns>
    Task<List<MedicationLogDto>?> GetMedicationLogsAsync(
        string medicationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        MedicationLogStatus? status = null);

    /// <summary>
    /// Gets a specific medication log by ID.
    /// </summary>
    /// <param name="id">Medication log ID.</param>
    /// <returns>Medication log details.</returns>
    Task<MedicationLogDto?> GetMedicationLogByIdAsync(string id);

    /// <summary>
    /// Logs a medication dose (records that medication was taken).
    /// </summary>
    /// <param name="medicationLog">Medication log data.</param>
    /// <returns>Created medication log details.</returns>
    Task<MedicationLogDto?> LogMedicationAsync(MedicationLogDto medicationLog);

    /// <summary>
    /// Updates an existing medication log.
    /// </summary>
    /// <param name="id">Medication log ID.</param>
    /// <param name="medicationLog">Updated medication log data.</param>
    /// <returns>Updated medication log details.</returns>
    Task<MedicationLogDto?> UpdateMedicationLogAsync(string id, MedicationLogDto medicationLog);

    /// <summary>
    /// Deletes a medication log (soft delete).
    /// </summary>
    /// <param name="id">Medication log ID.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteMedicationLogAsync(string id);

    /// <summary>
    /// Gets today's medication logs for all medications.
    /// </summary>
    /// <returns>List of today's medication logs.</returns>
    Task<List<MedicationLogDto>?> GetTodaysLogsAsync();

    /// <summary>
    /// Calculates adherence rate for a medication over a date range.
    /// </summary>
    /// <param name="medicationId">Medication ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>Adherence percentage (0-100).</returns>
    Task<decimal?> GetAdherenceRateAsync(string medicationId, DateTime fromDate, DateTime toDate);
}

/// <summary>
/// Data transfer object for medication log information.
/// </summary>
public sealed class MedicationLogDto
{
    public string Id { get; set; } = string.Empty;
    public string MedicationId { get; set; } = string.Empty;
    public string MedicationName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public DateTime? ActualTime { get; set; }
    public MedicationLogStatus Status { get; set; }
    public decimal? ActualDosage { get; set; }
    public string? ActualDosageUnit { get; set; }
    public string? Reason { get; set; }
    public string? SideEffects { get; set; }
    public string? Notes { get; set; }
    public bool? TakenWithFood { get; set; }
    public string? FoodDetails { get; set; }
    public int TimeVarianceMinutes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Variance tracking fields (T035)
    /// <summary>
    /// Expected dosage from the active pattern on ScheduledTime date.
    /// NULL if no pattern was active.
    /// </summary>
    public decimal? ExpectedDosage { get; set; }

    /// <summary>
    /// Position in the dosage pattern cycle (1-based).
    /// Example: Day 3 of a 6-day pattern.
    /// NULL if no pattern was active.
    /// </summary>
    public int? PatternDayNumber { get; set; }

    /// <summary>
    /// Indicates whether actual dosage differs from expected dosage (variance > 0.01mg).
    /// </summary>
    public bool HasVariance { get; set; }

    /// <summary>
    /// Variance amount (actual - expected).
    /// Positive = took more than expected, negative = took less.
    /// NULL if no expected dosage is set.
    /// </summary>
    public decimal? VarianceAmount { get; set; }

    /// <summary>
    /// Variance percentage ((actual - expected) / expected * 100).
    /// Example: -25% means took 25% less than expected.
    /// NULL if no expected dosage or expected dosage is 0.
    /// </summary>
    public decimal? VariancePercentage { get; set; }
}
