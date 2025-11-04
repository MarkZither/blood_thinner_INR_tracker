namespace BloodThinnerTracker.Shared.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a new medication dosage pattern.
/// Used by POST /api/medications/{medicationId}/patterns endpoint.
/// </summary>
/// <remarks>
/// ⚠️ MEDICAL DATA: This DTO creates variable-dosage schedules for blood thinner medications.
/// Validation is critical for patient safety.
///
/// Usage:
/// ```csharp
/// var request = new CreateDosagePatternRequest
/// {
///     PatternSequence = new List&lt;decimal&gt; { 4.0m, 4.0m, 3.0m, 4.0m, 3.0m, 3.0m },
///     StartDate = DateTime.UtcNow.Date,
///     Notes = "Adjusted based on INR 3.2",
///     ClosePreviousPattern = true
/// };
/// ```
/// </remarks>
public class CreateDosagePatternRequest
{
    /// <summary>
    /// Gets or sets the array of dosages forming the repeating pattern.
    /// Example: [4.0, 4.0, 3.0] for 3-day cycle.
    /// </summary>
    /// <remarks>
    /// ⚠️ PATTERN SEQUENCE: Core of the dosage pattern feature.
    ///
    /// Validation:
    /// - Must contain 1-365 dosages
    /// - Each dosage: 0.1 &lt;= value &lt;= 1000 (general)
    /// - Warfarin medications: Each dosage &lt;= 20mg (FluentValidation rule)
    /// - All values must be positive decimals
    ///
    /// Examples:
    /// - Simple alternating: [4.0, 3.0] (2-day cycle)
    /// - Complex weekly: [4.0, 4.0, 3.0, 4.0, 3.0, 3.0, 3.0] (7-day cycle)
    /// - Gradual taper: [5.0, 4.5, 4.0, 3.5, 3.0] (5-day taper)
    /// </remarks>
    [Required(ErrorMessage = "Pattern sequence is required")]
    [MinLength(1, ErrorMessage = "Pattern must have at least 1 dosage")]
    [MaxLength(365, ErrorMessage = "Pattern cannot exceed 365 dosages")]
    public List<decimal> PatternSequence { get; set; } = new();

    /// <summary>
    /// Gets or sets the date when this pattern becomes active (inclusive).
    /// </summary>
    /// <remarks>
    /// ⚠️ START DATE: Temporal validity anchor.
    ///
    /// Validation:
    /// - Cannot be more than 1 year in the past (prevents accidental backdating)
    /// - Cannot be more than 7 days in the future (prevents premature scheduling)
    /// - Must not overlap with other active patterns unless ClosePreviousPattern = true
    ///
    /// Backdating Scenario (FR-011):
    /// - User realizes they started new pattern 3 days ago but forgot to log
    /// - Set StartDate to 3 days ago
    /// - If &gt;7 days past, UI shows confirmation dialog (FR-011)
    /// - API accepts any valid past date (no rejection)
    /// </remarks>
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the optional end date for this pattern (inclusive).
    /// Leave null for ongoing pattern.
    /// </summary>
    /// <remarks>
    /// ⚠️ END DATE: Temporal validity termination.
    ///
    /// Validation:
    /// - Must be &gt;= StartDate if provided
    /// - NULL indicates currently active pattern (most common)
    ///
    /// When to set EndDate:
    /// - Usually left NULL when creating new pattern
    /// - Set automatically by API when ClosePreviousPattern = true (closes old pattern)
    /// - Can be set explicitly to create historical pattern (e.g., data migration)
    ///
    /// Multiple Active Patterns Prevention:
    /// - Only one pattern per medication should have EndDate = NULL
    /// - Enforced by validation logic in controller
    /// - ClosePreviousPattern ensures automatic closure
    /// </remarks>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets optional user notes about this pattern change.
    /// Example: "Adjusted based on INR 3.2", "Reduced winter dosing"
    /// </summary>
    /// <remarks>
    /// Useful for:
    /// - Documenting reason for pattern change
    /// - Recording healthcare provider instructions
    /// - Tracking seasonal adjustments
    /// - Audit trail for dosage modifications
    /// </remarks>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically close the previous active pattern.
    /// If true, sets the EndDate of the current active pattern to StartDate - 1 day.
    /// Default: true
    /// </summary>
    /// <remarks>
    /// ⚠️ PATTERN CLOSURE: Critical for temporal accuracy (FR-003).
    ///
    /// Behavior:
    /// - true (default): Finds current active pattern (EndDate = NULL) and sets:
    ///   `EndDate = request.StartDate.AddDays(-1)`
    /// - false: Creates new pattern without modifying existing patterns
    ///   (Use case: Data migration, historical pattern reconstruction)
    ///
    /// Example:
    /// - Old pattern: StartDate = 2025-10-01, EndDate = NULL
    /// - New pattern: StartDate = 2025-11-04, ClosePreviousPattern = true
    /// - Result:
    ///   * Old pattern: StartDate = 2025-10-01, EndDate = 2025-11-03
    ///   * New pattern: StartDate = 2025-11-04, EndDate = NULL
    ///
    /// Edge Case:
    /// - If no active pattern exists, this flag is ignored (no error)
    /// </remarks>
    public bool ClosePreviousPattern { get; set; } = true;
}
