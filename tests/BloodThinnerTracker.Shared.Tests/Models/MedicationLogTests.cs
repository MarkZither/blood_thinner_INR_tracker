using BloodThinnerTracker.Shared.Models;
using FluentAssertions;
using Xunit;

namespace BloodThinnerTracker.Shared.Tests.Models;

/// <summary>
/// Unit tests for MedicationLog variance tracking functionality.
/// Tests HasVariance, VarianceAmount, and VariancePercentage computed properties.
/// </summary>
public class MedicationLogTests
{
    #region HasVariance Tests

    [Fact]
    public void HasVariance_WhenActualMatchesExpected_ReturnsFalse()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 4.0m
        };

        // Act
        var hasVariance = log.HasVariance;

        // Assert
        hasVariance.Should().BeFalse("actual dosage matches expected dosage exactly");
    }

    [Fact]
    public void HasVariance_WhenDifferenceWithinTolerance_ReturnsFalse()
    {
        // Arrange - 0.01mg difference (exactly at tolerance threshold)
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 4.01m
        };

        // Act
        var hasVariance = log.HasVariance;

        // Assert
        hasVariance.Should().BeFalse("difference of 0.01mg is within tolerance threshold");
    }

    [Fact]
    public void HasVariance_WhenDifferenceExceedsTolerance_ReturnsTrue()
    {
        // Arrange - 0.02mg difference (exceeds tolerance)
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 4.02m
        };

        // Act
        var hasVariance = log.HasVariance;

        // Assert
        hasVariance.Should().BeTrue("difference of 0.02mg exceeds 0.01mg tolerance");
    }

    [Fact]
    public void HasVariance_WhenActualLessThanExpected_ReturnsTrue()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 3.5m
        };

        // Act
        var hasVariance = log.HasVariance;

        // Assert
        hasVariance.Should().BeTrue("actual dosage is 0.5mg less than expected");
    }

    [Fact]
    public void HasVariance_WhenExpectedIsNull_ReturnsFalse()
    {
        // Arrange - No pattern, so no expected dosage
        var log = new MedicationLog
        {
            ExpectedDosage = null,
            ActualDosage = 4.0m
        };

        // Act
        var hasVariance = log.HasVariance;

        // Assert
        hasVariance.Should().BeFalse("cannot calculate variance without expected dosage");
    }

    [Theory]
    [InlineData(5.0, 5.5, true)]   // +0.5mg variance
    [InlineData(5.0, 4.5, true)]   // -0.5mg variance
    [InlineData(3.0, 3.0, false)]  // No variance
    [InlineData(2.5, 2.51, false)] // +0.01mg (exactly at threshold, not > 0.01)
    [InlineData(2.5, 2.511, true)] // +0.011mg (exceeds threshold)
    [InlineData(2.5, 2.505, false)] // +0.005mg (within tolerance)
    public void HasVariance_VariousScenarios_ReturnsExpectedResult(
        decimal expected, decimal actual, bool shouldHaveVariance)
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = expected,
            ActualDosage = actual
        };

        // Act
        var hasVariance = log.HasVariance;

        // Assert
        hasVariance.Should().Be(shouldHaveVariance,
            $"expected {expected}mg vs actual {actual}mg should {(shouldHaveVariance ? "" : "not ")}have variance");
    }

    #endregion

    #region VarianceAmount Tests

    [Fact]
    public void VarianceAmount_WhenActualGreaterThanExpected_ReturnsPositive()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 4.5m
        };

        // Act
        var variance = log.VarianceAmount;

        // Assert
        variance.Should().Be(0.5m, "actual is 0.5mg more than expected");
    }

    [Fact]
    public void VarianceAmount_WhenActualLessThanExpected_ReturnsNegative()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 3.5m
        };

        // Act
        var variance = log.VarianceAmount;

        // Assert
        variance.Should().Be(-0.5m, "actual is 0.5mg less than expected");
    }

    [Fact]
    public void VarianceAmount_WhenActualMatchesExpected_ReturnsZero()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 4.0m
        };

        // Act
        var variance = log.VarianceAmount;

        // Assert
        variance.Should().Be(0m, "actual matches expected exactly");
    }

    [Fact]
    public void VarianceAmount_WhenExpectedIsNull_ReturnsNull()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = null,
            ActualDosage = 4.0m
        };

        // Act
        var variance = log.VarianceAmount;

        // Assert
        variance.Should().BeNull("cannot calculate variance without expected dosage");
    }

    [Theory]
    [InlineData(5.0, 6.0, 1.0)]    // +1.0mg
    [InlineData(5.0, 4.0, -1.0)]   // -1.0mg
    [InlineData(2.5, 2.75, 0.25)]  // +0.25mg
    [InlineData(3.0, 2.5, -0.5)]   // -0.5mg
    public void VarianceAmount_VariousScenarios_CalculatesCorrectly(
        decimal expected, decimal actual, decimal expectedVariance)
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = expected,
            ActualDosage = actual
        };

        // Act
        var variance = log.VarianceAmount;

        // Assert
        variance.Should().Be(expectedVariance,
            $"variance from {expected}mg to {actual}mg should be {expectedVariance}mg");
    }

    #endregion

    #region VariancePercentage Tests

    [Fact]
    public void VariancePercentage_WhenActual25PercentHigher_Returns25()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 5.0m // 25% increase
        };

        // Act
        var percentage = log.VariancePercentage;

        // Assert
        percentage.Should().Be(25.0m, "5.0mg is 25% more than 4.0mg");
    }

    [Fact]
    public void VariancePercentage_WhenActual25PercentLower_ReturnsNegative25()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 3.0m // 25% decrease
        };

        // Act
        var percentage = log.VariancePercentage;

        // Assert
        percentage.Should().Be(-25.0m, "3.0mg is 25% less than 4.0mg");
    }

    [Fact]
    public void VariancePercentage_WhenActualMatchesExpected_ReturnsZero()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = 4.0m,
            ActualDosage = 4.0m
        };

        // Act
        var percentage = log.VariancePercentage;

        // Assert
        percentage.Should().Be(0m, "no variance means 0% difference");
    }

    [Fact]
    public void VariancePercentage_WhenExpectedIsNull_ReturnsNull()
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = null,
            ActualDosage = 4.0m
        };

        // Act
        var percentage = log.VariancePercentage;

        // Assert
        percentage.Should().BeNull("cannot calculate percentage without expected dosage");
    }

    [Fact]
    public void VariancePercentage_WhenExpectedIsZero_ReturnsNull()
    {
        // Arrange - Edge case: expected dosage is 0 (should not happen in practice)
        var log = new MedicationLog
        {
            ExpectedDosage = 0m,
            ActualDosage = 4.0m
        };

        // Act
        var percentage = log.VariancePercentage;

        // Assert
        percentage.Should().BeNull("cannot divide by zero when expected dosage is 0");
    }

    [Theory]
    [InlineData(4.0, 5.0, 25.0)]      // +25%
    [InlineData(4.0, 3.0, -25.0)]     // -25%
    [InlineData(5.0, 5.5, 10.0)]      // +10%
    [InlineData(5.0, 4.5, -10.0)]     // -10%
    [InlineData(2.0, 3.0, 50.0)]      // +50%
    [InlineData(10.0, 5.0, -50.0)]    // -50%
    [InlineData(3.0, 3.3, 10.0)]      // +10%
    public void VariancePercentage_VariousScenarios_CalculatesCorrectly(
        decimal expected, decimal actual, decimal expectedPercentage)
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = expected,
            ActualDosage = actual
        };

        // Act
        var percentage = log.VariancePercentage;

        // Assert
        percentage.Should().BeApproximately(expectedPercentage, 0.1m,
            $"variance from {expected}mg to {actual}mg should be approximately {expectedPercentage}%");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void VarianceTracking_CompleteScenario_AllPropertiesCalculateCorrectly()
    {
        // Arrange - Patient took 3.5mg instead of expected 4.0mg
        var log = new MedicationLog
        {
            MedicationId = 100,
            UserId = 1,
            ScheduledTime = DateTime.UtcNow,
            ActualTime = DateTime.UtcNow,
            ExpectedDosage = 4.0m,
            ActualDosage = 3.5m,
            PatternDayNumber = 2,
            DosagePatternId = 50,
            Notes = "Took less due to side effects"
        };

        // Act & Assert - All variance properties
        log.HasVariance.Should().BeTrue("actual differs from expected by 0.5mg");
        log.VarianceAmount.Should().Be(-0.5m, "actual is 0.5mg less than expected");
        log.VariancePercentage.Should().BeApproximately(-12.5m, 0.1m,
            "3.5mg is 12.5% less than 4.0mg");
    }

    [Fact]
    public void VarianceTracking_NoPatternScenario_VariancePropertiesReturnNull()
    {
        // Arrange - Medication has no pattern, so ExpectedDosage is null
        var log = new MedicationLog
        {
            MedicationId = 100,
            UserId = 1,
            ScheduledTime = DateTime.UtcNow,
            ActualTime = DateTime.UtcNow,
            ExpectedDosage = null, // No pattern
            ActualDosage = 4.0m,
            PatternDayNumber = null,
            DosagePatternId = null
        };

        // Act & Assert
        log.HasVariance.Should().BeFalse("no expected dosage means no variance tracking");
        log.VarianceAmount.Should().BeNull("cannot calculate amount without expected dosage");
        log.VariancePercentage.Should().BeNull("cannot calculate percentage without expected dosage");
    }

    [Fact]
    public void VarianceTracking_PerfectAdherence_NoVarianceDetected()
    {
        // Arrange - Patient followed pattern exactly
        var log = new MedicationLog
        {
            MedicationId = 100,
            UserId = 1,
            ScheduledTime = DateTime.UtcNow,
            ActualTime = DateTime.UtcNow,
            ExpectedDosage = 4.0m,
            ActualDosage = 4.0m, // Perfect match
            PatternDayNumber = 1,
            DosagePatternId = 50
        };

        // Act & Assert
        log.HasVariance.Should().BeFalse("perfect adherence means no variance");
        log.VarianceAmount.Should().Be(0m, "zero difference");
        log.VariancePercentage.Should().Be(0m, "0% variance");
    }

    [Fact]
    public void VarianceTracking_SignificantOverdose_DetectedCorrectly()
    {
        // Arrange - Patient accidentally took double dose
        var log = new MedicationLog
        {
            MedicationId = 100,
            UserId = 1,
            ScheduledTime = DateTime.UtcNow,
            ActualTime = DateTime.UtcNow,
            ExpectedDosage = 4.0m,
            ActualDosage = 8.0m, // Double dose
            PatternDayNumber = 3,
            DosagePatternId = 50,
            Notes = "Accidentally took evening dose twice"
        };

        // Act & Assert - Should flag significant variance
        log.HasVariance.Should().BeTrue("significant overdose detected");
        log.VarianceAmount.Should().Be(4.0m, "took 4mg more than expected");
        log.VariancePercentage.Should().Be(100.0m, "100% overdose (double expected)");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void VarianceTracking_VerySmallDosages_HandlesCorrectly()
    {
        // Arrange - Very small dosages (e.g., pediatric or hormone medications)
        var log = new MedicationLog
        {
            ExpectedDosage = 0.5m,
            ActualDosage = 0.6m
        };

        // Act & Assert
        log.HasVariance.Should().BeTrue("0.1mg variance on 0.5mg base is significant");
        log.VarianceAmount.Should().Be(0.1m);
        log.VariancePercentage.Should().Be(20.0m, "0.6mg is 20% more than 0.5mg");
    }

    [Fact]
    public void VarianceTracking_LargeDosages_HandlesCorrectly()
    {
        // Arrange - Very large dosages
        var log = new MedicationLog
        {
            ExpectedDosage = 100.0m,
            ActualDosage = 105.0m
        };

        // Act & Assert
        log.HasVariance.Should().BeTrue("5mg variance on 100mg base");
        log.VarianceAmount.Should().Be(5.0m);
        log.VariancePercentage.Should().Be(5.0m, "105mg is 5% more than 100mg");
    }

    [Theory]
    [InlineData(4.0, 4.001, false)]  // Negligible difference
    [InlineData(4.0, 4.009, false)]  // Just under tolerance
    [InlineData(4.0, 4.011, true)]   // Just over tolerance
    [InlineData(4.0, 4.02, true)]    // Clearly over tolerance
    public void HasVariance_ToleranceThreshold_WorksCorrectly(
        decimal expected, decimal actual, bool shouldHaveVariance)
    {
        // Arrange
        var log = new MedicationLog
        {
            ExpectedDosage = expected,
            ActualDosage = actual
        };

        // Act
        var hasVariance = log.HasVariance;

        // Assert
        hasVariance.Should().Be(shouldHaveVariance,
            $"tolerance threshold at 0.01mg should {(shouldHaveVariance ? "flag" : "ignore")} " +
            $"difference of {Math.Abs(actual - expected)}mg");
    }

    #endregion
}
