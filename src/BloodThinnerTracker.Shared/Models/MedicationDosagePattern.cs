namespace BloodThinnerTracker.Shared.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a variable-dosage schedule with temporal validity for medication tracking.
/// Enables pattern history tracking and date-based dosage calculation.
/// </summary>
/// <remarks>
/// ⚠️ MEDICAL DATA: This entity stores dosage patterns for blood thinner medications.
/// Pattern calculation accuracy is critical for patient safety.
/// 
/// Temporal Pattern Tracking:
/// - Multiple patterns per medication allow tracking dosage adjustments over time
/// - StartDate and EndDate (nullable) enable temporal queries for any historical date
/// - Pattern sequences stored as JSON: [4.0, 4.0, 3.0, 4.0, 3.0, 3.0] (6-day cycle)
/// 
/// Pattern Calculation:
/// - Uses modulo arithmetic for O(1) performance regardless of pattern length
/// - Day numbers are 1-based (Day 1, Day 2, etc.) for user-friendly display
/// - GetDosageForDate handles date-to-day-number conversion automatically
/// </remarks>
[Table("MedicationDosagePatterns")]
public class MedicationDosagePattern : MedicalEntityBase
{
    /// <summary>
    /// Gets or sets the foreign key to parent Medication.
    /// </summary>
    /// <remarks>
    /// ⚠️ MEDICAL DATA ISOLATION: This FK ensures patterns belong to specific medications.
    /// Combined with Medication.UserId, enforces user data isolation.
    /// </remarks>
    [Required]
    public int MedicationId { get; set; }

    /// <summary>
    /// Gets or sets the repeating dosage pattern stored as JSON array of decimals.
    /// Example: [4.0, 4.0, 3.0, 4.0, 3.0, 3.0] for 6-day cycle.
    /// </summary>
    /// <remarks>
    /// ⚠️ MEDICAL DATA: Dosage values are in the medication's DosageUnit (typically mg).
    /// Validation rules:
    /// - Must contain 1-365 dosages
    /// - Each dosage must be &gt;= 0 and &lt;= 100mg (validated in FluentValidation)
    /// - Empty patterns are invalid and will throw exceptions
    /// 
    /// Database Storage:
    /// - PostgreSQL: JSONB column (indexed, queryable)
    /// - SQLite: TEXT column with JSON string
    /// - EF Core handles serialization/deserialization automatically
    /// </remarks>
    [Required]
    [Column(TypeName = "jsonb")] // PostgreSQL JSONB, SQLite will use TEXT
    public List<decimal> PatternSequence { get; set; } = new();

    /// <summary>
    /// Gets or sets the date when this pattern becomes active (inclusive).
    /// </summary>
    /// <remarks>
    /// ⚠️ TEMPORAL TRACKING: StartDate enables historical pattern queries.
    /// - Must be &lt;= EndDate if EndDate is set
    /// - Can be backdated to correct historical data (with user confirmation if &gt;7 days past)
    /// - All dates stored in UTC, converted to user timezone for display
    /// </remarks>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the date when this pattern ends (inclusive). 
    /// NULL indicates currently active pattern.
    /// </summary>
    /// <remarks>
    /// ⚠️ TEMPORAL TRACKING: EndDate marks pattern closure.
    /// - NULL = pattern is currently active
    /// - Non-NULL = pattern is historical (superseded by newer pattern)
    /// - When creating new pattern, previous pattern's EndDate = new StartDate - 1 day
    /// - Enables accurate historical dosage calculations for past medication logs
    /// </remarks>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets optional user-provided description for this pattern change.
    /// Example: "Reduced winter dosing", "Adjusted based on INR 3.2"
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// Gets or sets the parent medication this pattern belongs to.
    /// </summary>
    /// <remarks>
    /// ⚠️ REQUIRED NAVIGATION: Virtual for EF Core lazy loading.
    /// Cascade delete configured: deleting medication deletes all its patterns.
    /// </remarks>
    public virtual Medication Medication { get; set; } = null!;

    // Computed Properties

    /// <summary>
    /// Gets the number of days in this pattern cycle.
    /// Returns 0 if PatternSequence is null or empty.
    /// </summary>
    /// <remarks>
    /// Used for modulo arithmetic in GetDosageForDay calculation.
    /// Not stored in database - computed on-the-fly.
    /// </remarks>
    [NotMapped]
    public int PatternLength => PatternSequence?.Count ?? 0;

    /// <summary>
    /// Gets a value indicating whether this pattern is currently active.
    /// Active = EndDate is NULL OR EndDate &gt;= today's date.
    /// </summary>
    /// <remarks>
    /// ⚠️ TEMPORAL LOGIC: Uses UTC date comparison.
    /// - Active patterns can be modified (set EndDate to close them)
    /// - Historical patterns (EndDate in past) are read-only
    /// - Multiple active patterns for same medication are prevented by validation
    /// </remarks>
    [NotMapped]
    public bool IsActive => EndDate == null || EndDate >= DateTime.UtcNow.Date;

    /// <summary>
    /// Gets the average dosage across all days in the pattern.
    /// Returns 0 if PatternSequence is empty.
    /// </summary>
    /// <remarks>
    /// Useful for:
    /// - Comparing patterns (e.g., "New pattern averages 3.67mg vs old 3.33mg")
    /// - Calculating weekly/monthly medication supply needs
    /// - Displaying pattern summary in UI
    /// </remarks>
    [NotMapped]
    public decimal AverageDosage => PatternSequence?.Count > 0 
        ? PatternSequence.Average() 
        : 0;

    // Pattern Calculation Methods

    /// <summary>
    /// Calculates the dosage for a specific day number in the pattern (1-based).
    /// </summary>
    /// <param name="dayNumber">Day number starting from 1 (Day 1, Day 2, etc.).</param>
    /// <returns>Dosage for that day in the pattern cycle.</returns>
    /// <exception cref="InvalidOperationException">Thrown if PatternSequence is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if dayNumber &lt; 1.</exception>
    /// <remarks>
    /// ⚠️ ALGORITHM: Uses modulo arithmetic for O(1) performance.
    /// 
    /// Example: Pattern [4.0, 4.0, 3.0] (3-day cycle)
    /// - GetDosageForDay(1) → 4.0 (Day 1)
    /// - GetDosageForDay(2) → 4.0 (Day 2)
    /// - GetDosageForDay(3) → 3.0 (Day 3)
    /// - GetDosageForDay(4) → 4.0 (Day 1 again - pattern repeats)
    /// - GetDosageForDay(100) → 4.0 (100 mod 3 = 1, so Day 1)
    /// 
    /// Day numbers are 1-based for user-friendly display.
    /// Internally converts to 0-based array indexing.
    /// </remarks>
    public decimal GetDosageForDay(int dayNumber)
    {
        if (PatternSequence == null || PatternSequence.Count == 0)
            throw new InvalidOperationException("Pattern sequence is empty");

        if (dayNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(dayNumber), "Day number must be >= 1");

        // Convert to zero-based index using modulo
        // Example: Day 1 → index 0, Day 4 (in 3-day pattern) → index 0
        int zeroBasedIndex = (dayNumber - 1) % PatternSequence.Count;
        return PatternSequence[zeroBasedIndex];
    }

    /// <summary>
    /// Calculates the dosage for a specific date.
    /// </summary>
    /// <param name="targetDate">Date to calculate dosage for.</param>
    /// <returns>Dosage for that date, or null if date is outside pattern validity.</returns>
    /// <remarks>
    /// ⚠️ TEMPORAL LOGIC: Validates date is within [StartDate, EndDate] range.
    /// 
    /// Returns null if:
    /// - targetDate &lt; StartDate (pattern not yet active)
    /// - targetDate &gt; EndDate (pattern already closed)
    /// 
    /// Otherwise:
    /// 1. Calculate days since StartDate
    /// 2. Convert to pattern day number (1-based)
    /// 3. Return dosage for that day using GetDosageForDay()
    /// 
    /// Example: Pattern [4.0, 4.0, 3.0] starting 2025-11-01
    /// - GetDosageForDate(2025-11-01) → 4.0 (Day 1, 0 days since start)
    /// - GetDosageForDate(2025-11-02) → 4.0 (Day 2, 1 day since start)
    /// - GetDosageForDate(2025-11-03) → 3.0 (Day 3, 2 days since start)
    /// - GetDosageForDate(2025-11-04) → 4.0 (Day 1 again, 3 days since start)
    /// </remarks>
    public decimal? GetDosageForDate(DateTime targetDate)
    {
        // Check if date is within pattern validity period
        if (targetDate.Date < StartDate.Date)
            return null;

        if (EndDate.HasValue && targetDate.Date > EndDate.Value.Date)
            return null;

        // Calculate days since pattern start (0-based)
        int daysSinceStart = (targetDate.Date - StartDate.Date).Days;
        
        // Convert to 1-based day number (Day 1, Day 2, etc.)
        int dayNumber = (daysSinceStart % PatternLength) + 1;

        return GetDosageForDay(dayNumber);
    }

    /// <summary>
    /// Gets a human-readable pattern representation.
    /// </summary>
    /// <param name="unit">Dosage unit (default: "mg").</param>
    /// <returns>Formatted pattern string. Example: "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)"</returns>
    /// <remarks>
    /// Used for UI display and logging.
    /// Formats decimal values without trailing zeros (e.g., 4.0 → "4mg", 3.5 → "3.5mg").
    /// </remarks>
    public string GetDisplayPattern(string unit = "mg")
    {
        if (PatternSequence == null || PatternSequence.Count == 0)
            return "Empty pattern";

        var values = string.Join(", ", PatternSequence.Select(d => $"{d:0.##}{unit}"));
        return $"{values} ({PatternLength}-day cycle)";
    }
}
