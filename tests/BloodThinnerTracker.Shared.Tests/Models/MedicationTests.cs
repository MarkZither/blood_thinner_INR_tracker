using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Shared.Tests.Models;

/// <summary>
/// Unit tests for Medication.GetExpectedDosageForDate() method.
/// Tests pattern lookup, frequency-aware calculation, and fallback logic.
/// </summary>
public class MedicationTests
{
    #region Daily Frequency Tests

    [Fact]
    public void GetExpectedDosageForDate_DailyFrequency_UsesPatternForAllDays()
    {
        // Arrange
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.OnceDaily,
            patternSequence: new List<decimal> { 4.0m, 4.0m, 3.0m },
            startDate: new DateTime(2025, 11, 1)
        );

        // Act & Assert
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1)).Should().Be(4.0m); // Day 1
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 2)).Should().Be(4.0m); // Day 2
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 3)).Should().Be(3.0m); // Day 3
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 4)).Should().Be(4.0m); // Day 4 (wraps)
    }

    [Fact]
    public void GetExpectedDosageForDate_TwiceDaily_TreatsAsDaily()
    {
        // Arrange
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.TwiceDaily,
            patternSequence: new List<decimal> { 5.0m, 4.0m },
            startDate: new DateTime(2025, 11, 1)
        );

        // Act & Assert (frequency doesn't affect dosage amount, only timing)
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1)).Should().Be(5.0m);
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 2)).Should().Be(4.0m);
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 3)).Should().Be(5.0m);
    }

    #endregion

    #region Every Other Day Frequency Tests (FR-018)

    [Fact]
    public void GetExpectedDosageForDate_EveryOtherDay_SkipsIntermediateDays()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1); // Friday
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.EveryOtherDay,
            patternSequence: new List<decimal> { 4.0m, 3.0m },
            startDate: startDate
        );

        // Act & Assert (FR-018: Pattern applies to scheduled days only)
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1)).Should().Be(4.0m); // Scheduled (Day 0)
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 2)).Should().BeNull(); // NOT scheduled
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 3)).Should().Be(3.0m); // Scheduled (Day 1)
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 4)).Should().BeNull(); // NOT scheduled
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 5)).Should().Be(4.0m); // Scheduled (Day 2, wraps to pattern day 1)
    }

    [Fact]
    public void GetExpectedDosageForDate_EveryOtherDay_WithLongerPattern()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.EveryOtherDay,
            patternSequence: new List<decimal> { 5.0m, 4.0m, 3.0m }, // 3-day pattern
            startDate: startDate
        );

        // Act & Assert
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1)).Should().Be(5.0m); // Scheduled day 0 → pattern day 1
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 2)).Should().BeNull();
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 3)).Should().Be(4.0m); // Scheduled day 1 → pattern day 2
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 4)).Should().BeNull();
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 5)).Should().Be(3.0m); // Scheduled day 2 → pattern day 3
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 6)).Should().BeNull();
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 7)).Should().Be(5.0m); // Scheduled day 3 → pattern day 1 (wraps)
    }

    #endregion

    #region Weekly Frequency Tests (FR-018)

    [Fact]
    public void GetExpectedDosageForDate_Weekly_OnlyScheduledDayOfWeek()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 3); // Monday
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.Weekly,
            patternSequence: new List<decimal> { 10.0m, 15.0m },
            startDate: startDate
        );

        // Act & Assert (Only Mondays are scheduled)
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 3)).Should().Be(10.0m); // Monday week 1
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 4)).Should().BeNull(); // Tuesday
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 5)).Should().BeNull(); // Wednesday
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 10)).Should().Be(15.0m); // Monday week 2
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 17)).Should().Be(10.0m); // Monday week 3 (wraps)
    }

    #endregion

    #region As Needed / Custom Frequency Tests

    [Fact]
    public void GetExpectedDosageForDate_AsNeeded_AllDaysValid()
    {
        // Arrange
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.AsNeeded,
            patternSequence: new List<decimal> { 2.5m },
            startDate: new DateTime(2025, 11, 1)
        );

        // Act & Assert (All days return dosage for on-demand medications)
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1)).Should().Be(2.5m);
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 2)).Should().Be(2.5m);
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 10)).Should().Be(2.5m);
    }

    [Fact]
    public void GetExpectedDosageForDate_Custom_AllDaysValid()
    {
        // Arrange
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.Custom,
            patternSequence: new List<decimal> { 3.0m, 4.0m },
            startDate: new DateTime(2025, 11, 1)
        );

        // Act & Assert
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1)).Should().Be(3.0m);
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 2)).Should().Be(4.0m);
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 3)).Should().Be(3.0m);
    }

    #endregion

    #region Pattern Lookup Tests

    [Fact]
    public void GetExpectedDosageForDate_WithMultiplePatterns_UsesCorrectPattern()
    {
        // Arrange
        var medication = new Medication
        {
            Dosage = 5.0m,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            StartDate = new DateTime(2025, 10, 1),
            IsActive = true,
            DosagePatterns = new List<MedicationDosagePattern>
            {
                // Old pattern (ended)
                new MedicationDosagePattern
                {
                    PatternSequence = new List<decimal> { 5.0m, 5.0m, 4.0m },
                    StartDate = new DateTime(2025, 10, 1),
                    EndDate = new DateTime(2025, 10, 31)
                },
                // Current pattern (active)
                new MedicationDosagePattern
                {
                    PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m },
                    StartDate = new DateTime(2025, 11, 1),
                    EndDate = null
                }
            }
        };

        // Act & Assert
        // Should use old pattern for October dates
        // Oct 15 = 14 days since Oct 1 → (14 % 3) + 1 = Day 3 → 4.0m (third value in pattern)
        medication.GetExpectedDosageForDate(new DateTime(2025, 10, 15)).Should().Be(4.0m);

        // Should use new pattern for November dates
        // Nov 1 = 0 days since Nov 1 → Day 1 → 4.0m (first value)
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1)).Should().Be(4.0m);
        // Nov 3 = 2 days since Nov 1 → (2 % 3) + 1 = Day 3 → 3.0m (third value)
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 3)).Should().Be(3.0m);
    }

    [Fact]
    public void GetExpectedDosageForDate_BeforeAnyPattern_ReturnsNull()
    {
        // Arrange
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.OnceDaily,
            patternSequence: new List<decimal> { 4.0m, 3.0m },
            startDate: new DateTime(2025, 11, 1)
        );

        // Act
        var result = medication.GetExpectedDosageForDate(new DateTime(2025, 10, 31));

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Fallback to Fixed Dosage Tests

    [Fact]
    public void GetExpectedDosageForDate_WithoutPattern_UsesFixedDosage()
    {
        // Arrange
        var medication = new Medication
        {
            Dosage = 5.0m,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            StartDate = new DateTime(2025, 11, 1),
            IsActive = true,
            DosagePatterns = new List<MedicationDosagePattern>() // No patterns
        };

        // Act & Assert
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1)).Should().Be(5.0m);
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 10)).Should().Be(5.0m);
    }

    [Fact]
    public void GetExpectedDosageForDate_WithoutPattern_EveryOtherDay_RespectsFrequency()
    {
        // Arrange
        var medication = new Medication
        {
            Dosage = 10.0m,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.EveryOtherDay,
            StartDate = new DateTime(2025, 11, 1),
            IsActive = true,
            DosagePatterns = new List<MedicationDosagePattern>() // No patterns
        };

        // Act & Assert (Fixed dosage still respects frequency scheduling)
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1)).Should().Be(10.0m); // Scheduled
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 2)).Should().BeNull(); // Not scheduled
        medication.GetExpectedDosageForDate(new DateTime(2025, 11, 3)).Should().Be(10.0m); // Scheduled
    }

    #endregion

    #region Medication Date Range Tests

    [Fact]
    public void GetExpectedDosageForDate_BeforeMedicationStart_ReturnsNull()
    {
        // Arrange
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.OnceDaily,
            patternSequence: new List<decimal> { 4.0m, 3.0m },
            startDate: new DateTime(2025, 11, 1)
        );
        medication.StartDate = new DateTime(2025, 11, 1);

        // Act
        var result = medication.GetExpectedDosageForDate(new DateTime(2025, 10, 31));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetExpectedDosageForDate_AfterMedicationEnd_ReturnsNull()
    {
        // Arrange
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.OnceDaily,
            patternSequence: new List<decimal> { 4.0m, 3.0m },
            startDate: new DateTime(2025, 11, 1)
        );
        medication.StartDate = new DateTime(2025, 11, 1);
        medication.EndDate = new DateTime(2025, 11, 30);

        // Act
        var result = medication.GetExpectedDosageForDate(new DateTime(2025, 12, 1));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetExpectedDosageForDate_InactiveMedication_ReturnsNull()
    {
        // Arrange
        var medication = CreateMedicationWithPattern(
            frequency: MedicationFrequency.OnceDaily,
            patternSequence: new List<decimal> { 4.0m, 3.0m },
            startDate: new DateTime(2025, 11, 1)
        );
        medication.IsActive = false;

        // Act
        var result = medication.GetExpectedDosageForDate(new DateTime(2025, 11, 1));

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static Medication CreateMedicationWithPattern(
        MedicationFrequency frequency,
        List<decimal> patternSequence,
        DateTime startDate)
    {
        return new Medication
        {
            Name = "Test Medication",
            Dosage = 5.0m,
            DosageUnit = "mg",
            Frequency = frequency,
            StartDate = startDate,
            IsActive = true,
            DosagePatterns = new List<MedicationDosagePattern>
            {
                new MedicationDosagePattern
                {
                    PatternSequence = patternSequence,
                    StartDate = startDate,
                    EndDate = null // Active pattern
                }
            }
        };
    }

    #endregion
}
