using System.ComponentModel.DataAnnotations;

namespace BloodThinnerTracker.Shared.Models;

/// <summary>
/// Response DTO for dosage pattern data returned from GET endpoints.
/// Provides comprehensive pattern information including computed fields for display.
/// </summary>
/// <remarks>
/// This DTO is used by:
/// - GET /api/medications/{id}/patterns (pattern history)
/// - GET /api/medications/{id}/patterns/active (current active pattern)
/// - POST /api/medications/{id}/patterns (pattern creation response)
/// - PUT /api/medications/{id}/patterns/{patternId} (pattern update response)
///
/// The response includes both raw data fields and computed display fields to minimize
/// client-side calculation complexity. All temporal fields use UTC timestamps.
///
/// <para><strong>Medical Safety:</strong></para>
/// Pattern sequences represent CRITICAL MEDICATION DOSAGES. Always display the
/// <see cref="DisplayPattern"/> field to users for human verification. Never display
/// raw <see cref="PatternSequence"/> arrays without proper formatting.
/// </remarks>
public class DosagePatternResponse
{
    /// <summary>
    /// Unique identifier for the dosage pattern.
    /// </summary>
    /// <remarks>
    /// Internal database ID. Use for API operations (UPDATE, DELETE).
    /// Not suitable for public-facing URLs (use PublicId instead if exposed).
    /// </remarks>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// ID of the medication this pattern belongs to.
    /// </summary>
    [Required]
    public int MedicationId { get; set; }

    /// <summary>
    /// Array of dosages forming the repeating pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example: [4.0, 4.0, 3.0] = 4mg, 4mg, 3mg repeating cycle.
    /// </para>
    /// <para><strong>Medical Safety:</strong>
    /// Array length determines pattern cycle length (1-365 days).
    /// Each value must be within safe dosage range (validated at API layer).
    /// Pattern repeats indefinitely using modulo arithmetic.
    /// Display using <see cref="DisplayPattern"/> for user-facing output.
    /// </para>
    /// </remarks>
    [Required]
    [MinLength(1)]
    [MaxLength(365)]
    public List<decimal> PatternSequence { get; set; } = new();

    /// <summary>
    /// Number of days in the pattern cycle.
    /// </summary>
    /// <remarks>
    /// Computed from <see cref="PatternSequence"/> length.
    /// Example: [4, 4, 3] has PatternLength = 3.
    /// </remarks>
    [Required]
    public int PatternLength { get; set; }

    /// <summary>
    /// Date when this pattern becomes effective (inclusive).
    /// </summary>
    /// <remarks>
    /// <para>
    /// - Can be backdated to correct historical records
    /// - Must not overlap with other patterns for the same medication
    /// - Used for temporal queries: "What was the dosage on 2025-10-15?"
    /// </para>
    /// </remarks>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Date when this pattern ends (inclusive), or null if still active.
    /// </summary>
    /// <remarks>
    /// <para>
    /// - Null indicates the currently active pattern
    /// - Set automatically when creating a new pattern (if <c>ClosePreviousPattern=true</c>)
    /// - Must be >= <see cref="StartDate"/> if specified
    /// - Used for pattern history tracking and temporal queries
    /// </para>
    /// </remarks>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Optional medical notes about why this pattern was prescribed.
    /// </summary>
    /// <remarks>
    /// Examples: "Reduced winter dosing pattern", "Post-surgery adjustment",
    /// "INR stabilization protocol". Max 500 characters.
    /// </remarks>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Indicates if this pattern is currently active (EndDate is null).
    /// </summary>
    /// <remarks>
    /// Computed field: <c>IsActive = (EndDate == null)</c>.
    /// Only one pattern per medication should be active at any time.
    /// </remarks>
    [Required]
    public bool IsActive { get; set; }

    /// <summary>
    /// Average dosage across the entire pattern cycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Computed as: <c>Sum(PatternSequence) / PatternLength</c>
    /// </para>
    /// <para>
    /// Example: [4.0, 4.0, 3.0] → (4 + 4 + 3) / 3 = 3.67mg average
    /// </para>
    /// <para><strong>Medical Use:</strong></para>
    /// Useful for weekly/monthly dosage tracking and INR correlation analysis.
    /// Displayed to users for understanding overall medication exposure.
    /// </remarks>
    [Required]
    public decimal AverageDosage { get; set; }

    /// <summary>
    /// Human-readable formatted pattern with dosage unit and cycle indicator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Format: "{dosage1}{unit}, {dosage2}{unit}, ... ({patternLength}-day cycle)"
    /// </para>
    /// <para>
    /// Example: "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)"
    /// </para>
    /// <para><strong>Medical Safety:</strong>
    /// ALWAYS use this field for user-facing displays (not raw array).
    /// Includes dosage unit for clarity (mg, IU, etc.).
    /// Includes cycle length to prevent user confusion.
    /// Pre-formatted on server to ensure consistency.
    /// </para>
    /// </remarks>
    [Required]
    [MaxLength(1000)]
    public string DisplayPattern { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when this pattern was created.
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// UTC timestamp when this pattern was last modified.
    /// </summary>
    [Required]
    public DateTime ModifiedDate { get; set; }

    // ==========================================
    // EXTENDED FIELDS (Active Pattern Endpoint)
    // ==========================================

    /// <summary>
    /// Today's calculated dosage (only included in GET /patterns/active endpoint).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Calculated using modulo arithmetic:
    /// <c>PatternSequence[(DateTime.UtcNow.Date - StartDate.Date).Days % PatternLength]</c>
    /// </para>
    /// <para><strong>Medical Safety:</strong>
    /// Represents TODAY'S DOSE. Display prominently in medication reminder UI.
    /// </para>
    /// <para>
    /// Only populated for GET /api/medications/{id}/patterns/active.
    /// Null for pattern history endpoints.
    /// </para>
    /// </remarks>
    public decimal? TodaysDosage { get; set; }

    /// <summary>
    /// Today's position in the pattern cycle (1-based, only in active endpoint).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example: If pattern is [4, 4, 3] and today is day 5, TodaysPatternDay = 2
    /// (because 5 % 3 = 2, but displayed as 1-based).
    /// </para>
    /// <para>
    /// Only populated for GET /api/medications/{id}/patterns/active.
    /// Null for pattern history endpoints.
    /// </para>
    /// </remarks>
    public int? TodaysPatternDay { get; set; }
}
