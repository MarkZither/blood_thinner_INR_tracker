using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Shared.Tests.Models;

/// <summary>
/// Unit tests for MedicationDosagePattern entity.
/// Tests pattern calculation logic, display formatting, and validation.
/// </summary>
public class MedicationDosagePatternTests
{
    #region GetDosageForDay Tests

    [Fact]
    public void GetDosageForDay_WithValidDayNumber_ReturnsCorrectDosage()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m },
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act & Assert
        pattern.GetDosageForDay(1).Should().Be(4.0m); // Day 1
        pattern.GetDosageForDay(2).Should().Be(4.0m); // Day 2
        pattern.GetDosageForDay(3).Should().Be(3.0m); // Day 3
    }

    [Fact]
    public void GetDosageForDay_WithDayNumberBeyondPattern_WrapsCyclically()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m },
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act & Assert
        pattern.GetDosageForDay(4).Should().Be(4.0m); // Wraps to day 1
        pattern.GetDosageForDay(5).Should().Be(4.0m); // Wraps to day 2
        pattern.GetDosageForDay(6).Should().Be(3.0m); // Wraps to day 3
        pattern.GetDosageForDay(7).Should().Be(4.0m); // Wraps to day 1
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void GetDosageForDay_WithInvalidDayNumber_ThrowsArgumentOutOfRangeException(int dayNumber)
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 3.0m },
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => pattern.GetDosageForDay(dayNumber));
    }

    [Fact]
    public void GetDosageForDay_WithEmptyPattern_ThrowsInvalidOperationException()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal>(),
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => pattern.GetDosageForDay(1));
    }

    #endregion

    #region GetDosageForDate Tests

    [Fact]
    public void GetDosageForDate_OnStartDate_ReturnsFirstDayDosage()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 5.0m, 4.0m, 3.0m },
            StartDate = startDate
        };

        // Act
        var result = pattern.GetDosageForDate(startDate);

        // Assert
        result.Should().Be(5.0m);
    }

    [Fact]
    public void GetDosageForDate_MultipleDaysAfterStart_CalculatesCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m },
            StartDate = startDate
        };

        // Act & Assert
        pattern.GetDosageForDate(new DateTime(2025, 11, 1)).Should().Be(4.0m); // Day 0
        pattern.GetDosageForDate(new DateTime(2025, 11, 2)).Should().Be(4.0m); // Day 1
        pattern.GetDosageForDate(new DateTime(2025, 11, 3)).Should().Be(3.0m); // Day 2
        pattern.GetDosageForDate(new DateTime(2025, 11, 4)).Should().Be(4.0m); // Day 3 (wraps to 0)
        pattern.GetDosageForDate(new DateTime(2025, 11, 5)).Should().Be(4.0m); // Day 4 (wraps to 1)
    }

    [Fact]
    public void GetDosageForDate_BeforeStartDate_ReturnsNull()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 3.0m },
            StartDate = startDate
        };

        // Act
        var result = pattern.GetDosageForDate(new DateTime(2025, 10, 31));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDosageForDate_WithEndDate_ReturnsNullAfterEndDate()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 3.0m },
            StartDate = new DateTime(2025, 11, 1),
            EndDate = new DateTime(2025, 11, 10)
        };

        // Act
        var resultBefore = pattern.GetDosageForDate(new DateTime(2025, 11, 5));
        var resultOn = pattern.GetDosageForDate(new DateTime(2025, 11, 10));
        var resultAfter = pattern.GetDosageForDate(new DateTime(2025, 11, 11));

        // Assert
        resultBefore.Should().Be(4.0m); // Within range
        resultOn.Should().Be(3.0m); // On end date (inclusive)
        resultAfter.Should().BeNull(); // After end date
    }

    #endregion

    #region GetDisplayPattern Tests

    [Fact]
    public void GetDisplayPattern_WithStandardPattern_FormatsCorrectly()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m },
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act
        var result = pattern.GetDisplayPattern("mg");

        // Assert
        result.Should().Be("4mg, 4mg, 3mg (3-day cycle)");
    }

    [Fact]
    public void GetDisplayPattern_WithSingleValue_ShowsSingleDayCycle()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 5.0m },
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act
        var result = pattern.GetDisplayPattern("mg");

        // Assert
        result.Should().Be("5mg (1-day cycle)");
    }

    [Fact]
    public void GetDisplayPattern_WithDecimalValues_FormatsDecimals()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 2.5m, 3.75m, 1.25m },
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act
        var result = pattern.GetDisplayPattern("mg");

        // Assert
        result.Should().Be("2.5mg, 3.75mg, 1.25mg (3-day cycle)");
    }

    [Fact]
    public void GetDisplayPattern_WithDifferentUnit_UsesProvidedUnit()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 10.0m, 15.0m },
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act
        var result = pattern.GetDisplayPattern("IU");

        // Assert
        result.Should().Be("10IU, 15IU (2-day cycle)");
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void PatternLength_WithValidPattern_ReturnsCorrectCount()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m, 4.0m, 3.0m, 3.0m },
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act
        var length = pattern.PatternLength;

        // Assert
        length.Should().Be(6);
    }

    [Fact]
    public void IsActive_WithNullEndDate_ReturnsTrue()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 3.0m },
            StartDate = new DateTime(2025, 11, 1),
            EndDate = null
        };

        // Act
        var isActive = pattern.IsActive;

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WithEndDateInFuture_ReturnsTrue()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 3.0m },
            // Use relative dates so the test doesn't become time-sensitive
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30) // Future date, so pattern is still active
        };

        // Act
        var isActive = pattern.IsActive;

        // Assert
        isActive.Should().BeTrue("because EndDate is in the future");
    }

    [Fact]
    public void IsActive_WithEndDateInPast_ReturnsFalse()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 3.0m },
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31) // Past date, so pattern is expired
        };

        // Act
        var isActive = pattern.IsActive;

        // Assert
        isActive.Should().BeFalse("because EndDate is in the past");
    }

    [Fact]
    public void AverageDosage_WithStandardPattern_CalculatesCorrectly()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m }, // Average = 3.67
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act
        var average = pattern.AverageDosage;

        // Assert
        average.Should().BeApproximately(3.67m, 0.01m);
    }

    [Fact]
    public void AverageDosage_WithSingleValue_ReturnsThatValue()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal> { 5.0m },
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act
        var average = pattern.AverageDosage;

        // Assert
        average.Should().Be(5.0m);
    }

    [Fact]
    public void AverageDosage_WithEmptyPattern_ReturnsZero()
    {
        // Arrange
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = new List<decimal>(),
            StartDate = new DateTime(2025, 11, 1)
        };

        // Act
        var average = pattern.AverageDosage;

        // Assert
        average.Should().Be(0.0m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetDosageForDate_WithLongPattern_CalculatesCorrectlyOverMonths()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var longPattern = Enumerable.Range(1, 30).Select(i => (decimal)i).ToList();
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = longPattern,
            StartDate = startDate
        };

        // Act & Assert
        pattern.GetDosageForDate(new DateTime(2025, 11, 1)).Should().Be(1.0m); // Day 1
        pattern.GetDosageForDate(new DateTime(2025, 11, 30)).Should().Be(30.0m); // Day 30
        pattern.GetDosageForDate(new DateTime(2025, 12, 1)).Should().Be(1.0m); // Wraps to day 1
    }

    [Fact]
    public void GetDosageForDay_WithMaxPatternLength_HandlesCorrectly()
    {
        // Arrange
        var maxPattern = Enumerable.Range(1, 365).Select(i => (decimal)i).ToList();
        var pattern = new MedicationDosagePattern
        {
            PatternSequence = maxPattern,
            StartDate = new DateTime(2025, 1, 1)
        };

        // Act & Assert
        pattern.GetDosageForDay(1).Should().Be(1.0m);
        pattern.GetDosageForDay(365).Should().Be(365.0m);
        pattern.GetDosageForDay(366).Should().Be(1.0m); // Wraps
    }

    #endregion
}
