namespace BloodThinnerTracker.Web.Models;

/// <summary>
/// Defines the mode for entering medication dosage patterns.
/// </summary>
/// <remarks>
/// This enum supports two different user workflows for pattern entry:
/// - DateBased: User specifies the effective date, pattern starts at Day 1 on that date
/// - DayNumberBased: User specifies "Today is Day X", system back-calculates start date
///
/// The active mode is configured via feature flag in appsettings.json at "Features:PatternEntryMode".
/// Default mode is DayNumber for easier user experience (no date selection required).
/// </remarks>
public enum PatternEntryMode
{
    /// <summary>
    /// Date-based mode: User selects effective date, pattern starts Day 1.
    /// Example: "Pattern starts on November 4, 2025" → Day 1 = Nov 4.
    /// </summary>
    DateBased,

    /// <summary>
    /// Day-number-based mode: User specifies current day in pattern cycle.
    /// Example: "Today (Nov 4) is Day 3 of my pattern" → System calculates StartDate = Nov 2.
    /// </summary>
    DayNumber
}
