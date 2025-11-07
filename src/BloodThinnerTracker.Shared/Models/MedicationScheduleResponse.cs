using System.ComponentModel.DataAnnotations;

namespace BloodThinnerTracker.Shared.Models;

/// <summary>
/// Response DTO for medication dosage schedule calculation.
/// Returns a day-by-day dosage plan for 1-365 days based on active pattern.
/// </summary>
/// <remarks>
/// Used by GET /api/medications/{id}/schedule endpoint to provide users with
/// a visual calendar view of their upcoming medication dosages.
///
/// <para><strong>Medical Safety:</strong></para>
/// Schedules are CALCULATED PROJECTIONS based on current pattern. Users should
/// verify each dose before taking medication. Schedule calculations assume no
/// pattern changes during the projection period.
/// </remarks>
public class MedicationScheduleResponse
{
    /// <summary>
    /// ID of the medication being scheduled.
    /// </summary>
    [Required]
    public int MedicationId { get; set; }

    /// <summary>
    /// Display name of the medication.
    /// </summary>
    /// <remarks>
    /// Example: "Warfarin", "Apixaban", "Rivaroxaban"
    /// </remarks>
    [Required]
    [MaxLength(200)]
    public string MedicationName { get; set; } = string.Empty;

    /// <summary>
    /// Dosage unit for all values in the schedule.
    /// </summary>
    /// <remarks>
    /// Example: "mg", "IU", "mcg". Used for display consistency.
    /// </remarks>
    [Required]
    [MaxLength(20)]
    public string DosageUnit { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the schedule (inclusive).
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the schedule (inclusive).
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Number of days in the schedule.
    /// </summary>
    /// <remarks>
    /// Should match <c>(EndDate - StartDate).Days + 1</c> and the length of
    /// <see cref="Schedule"/> array.
    /// </remarks>
    [Required]
    [Range(1, 365)]
    public int TotalDays { get; set; }

    /// <summary>
    /// The active dosage pattern used to generate this schedule.
    /// </summary>
    [Required]
    public PatternSummary CurrentPattern { get; set; } = new();

    /// <summary>
    /// Statistical summary of the dosage schedule.
    /// </summary>
    [Required]
    public ScheduleSummary Summary { get; set; } = new();

    /// <summary>
    /// Day-by-day dosage schedule entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Array length should equal <see cref="TotalDays"/>.
    /// Ordered chronologically from <see cref="StartDate"/> to <see cref="EndDate"/>.
    /// </para>
    /// <para><strong>Medical Safety:</strong>
    /// Each entry represents a SINGLE DAY'S DOSE. Display with clear date labels
    /// to prevent dosing errors. Include visual indicators for pattern changes.
    /// </para>
    /// </remarks>
    [Required]
    [MinLength(1)]
    [MaxLength(365)]
    public List<ScheduleEntry> Schedule { get; set; } = new();
}

/// <summary>
/// Summary of the dosage pattern used to generate the schedule.
/// </summary>
public class PatternSummary
{
    /// <summary>
    /// Pattern ID.
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Pattern sequence array.
    /// </summary>
    /// <remarks>
    /// Example: [4.0, 4.0, 3.0, 4.0, 3.0, 3.0]
    /// </remarks>
    [Required]
    [MinLength(1)]
    [MaxLength(365)]
    public List<decimal> PatternSequence { get; set; } = new();

    /// <summary>
    /// Number of days in the pattern cycle.
    /// </summary>
    [Required]
    public int PatternLength { get; set; }

    /// <summary>
    /// Pattern start date.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Human-readable pattern display.
    /// </summary>
    /// <remarks>
    /// Example: "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)"
    /// </remarks>
    [Required]
    [MaxLength(1000)]
    public string DisplayPattern { get; set; } = string.Empty;
}

/// <summary>
/// Statistical summary of dosages across the schedule period.
/// </summary>
/// <remarks>
/// Useful for weekly/monthly dosage tracking and INR correlation analysis.
/// </remarks>
public class ScheduleSummary
{
    /// <summary>
    /// Sum of all dosages in the schedule period.
    /// </summary>
    /// <remarks>
    /// Example: 28-day schedule with average 3.67mg = 102.76mg total.
    /// Used for monthly medication consumption tracking.
    /// </remarks>
    [Required]
    public decimal TotalDosage { get; set; }

    /// <summary>
    /// Average daily dosage across the schedule period.
    /// </summary>
    /// <remarks>
    /// Calculated as: <c>TotalDosage / TotalDays</c>.
    /// May differ slightly from pattern average if period is not an exact multiple of pattern length.
    /// </remarks>
    [Required]
    public decimal AverageDailyDosage { get; set; }

    /// <summary>
    /// Lowest dosage in the schedule period.
    /// </summary>
    [Required]
    public decimal MinDosage { get; set; }

    /// <summary>
    /// Highest dosage in the schedule period.
    /// </summary>
    [Required]
    public decimal MaxDosage { get; set; }

    /// <summary>
    /// Number of complete + partial pattern cycles in the period.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example: 28 days with 6-day pattern = 4.67 cycles (4 complete + 0.67 partial).
    /// </para>
    /// <para>
    /// Calculated as: <c>(decimal)TotalDays / PatternLength</c>.
    /// </para>
    /// </remarks>
    [Required]
    public decimal PatternCycles { get; set; }
}

/// <summary>
/// Single day's dosage information in the schedule.
/// </summary>
public class ScheduleEntry
{
    /// <summary>
    /// Date for this schedule entry.
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Day of week for user-friendly display.
    /// </summary>
    /// <remarks>
    /// Example: "Monday", "Tuesday", "Wednesday"
    /// </remarks>
    [Required]
    [MaxLength(20)]
    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>
    /// Dosage for this day.
    /// </summary>
    /// <remarks>
    /// <para><strong>Medical Safety:</strong>
    /// THIS IS THE DOSE TO TAKE ON THIS DATE. Display prominently in UI.
    /// </para>
    /// </remarks>
    [Required]
    public decimal Dosage { get; set; }

    /// <summary>
    /// Position in the pattern cycle (1-based).
    /// </summary>
    /// <remarks>
    /// Example: If pattern is [4, 4, 3] and this is day 5, PatternDay = 2
    /// (because 5 % 3 = 2, but 1-indexed for display).
    /// </remarks>
    [Required]
    public int PatternDay { get; set; }

    /// <summary>
    /// Total length of the pattern cycle.
    /// </summary>
    /// <remarks>
    /// Included in each entry for client-side display logic.
    /// Example: "Day 2 of 6" display.
    /// </remarks>
    [Required]
    public int PatternLength { get; set; }

    /// <summary>
    /// Indicates if a new dosage pattern starts on this day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// True when:
    /// New pattern StartDate matches this date OR
    /// Previous pattern EndDate matches day before this date.
    /// </para>
    /// <para><strong>Medical Safety:</strong>
    /// Display visual indicator (icon, color, banner) when true to alert users
    /// of dosing changes. Include <see cref="PatternChangeNote"/> in UI.
    /// </para>
    /// </remarks>
    [Required]
    public bool IsPatternChange { get; set; }

    /// <summary>
    /// Description of pattern change (only present when IsPatternChange = true).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example: "New pattern starts: 4mg, 4mg, 3mg, 4mg, 3mg, 3mg"
    /// </para>
    /// <para>
    /// Null when <see cref="IsPatternChange"/> = false.
    /// </para>
    /// </remarks>
    [MaxLength(500)]
    public string? PatternChangeNote { get; set; }
}
