using BloodThinnerTracker.Shared.Models;
using FluentAssertions;
using Xunit;

namespace BloodThinnerTracker.Shared.Tests.Models;

/// <summary>
/// Unit tests for Medication frequency-related methods.
/// Tests IsScheduledMedicationDay() and GetScheduledDayNumber() logic for various frequencies.
/// </summary>
public class MedicationFrequencyTests
{
    #region IsScheduledMedicationDay Tests

    [Theory]
    [InlineData(0)]  // Day 0 (start date) - scheduled
    [InlineData(2)]  // Day 2 - scheduled
    [InlineData(4)]  // Day 4 - scheduled
    [InlineData(10)] // Day 10 - scheduled
    public void IsScheduledMedicationDay_EveryOtherDay_EvenDaysAreScheduled(int daysAfterStart)
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = new Medication
        {
            Frequency = MedicationFrequency.EveryOtherDay,
            StartDate = startDate,
            IsActive = true
        };
        var targetDate = startDate.AddDays(daysAfterStart);

        // Act - Using reflection to test private method
        var method = typeof(Medication).GetMethod("IsScheduledMedicationDay",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)method!.Invoke(medication, new object[] { targetDate })!;

        // Assert
        result.Should().BeTrue($"day {daysAfterStart} should be scheduled for EveryOtherDay frequency");
    }

    [Theory]
    [InlineData(1)]  // Day 1 - not scheduled
    [InlineData(3)]  // Day 3 - not scheduled
    [InlineData(5)]  // Day 5 - not scheduled
    [InlineData(9)]  // Day 9 - not scheduled
    public void IsScheduledMedicationDay_EveryOtherDay_OddDaysAreNotScheduled(int daysAfterStart)
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = new Medication
        {
            Frequency = MedicationFrequency.EveryOtherDay,
            StartDate = startDate,
            IsActive = true
        };
        var targetDate = startDate.AddDays(daysAfterStart);

        // Act
        var method = typeof(Medication).GetMethod("IsScheduledMedicationDay",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)method!.Invoke(medication, new object[] { targetDate })!;

        // Assert
        result.Should().BeFalse($"day {daysAfterStart} should not be scheduled for EveryOtherDay frequency");
    }

    [Fact]
    public void IsScheduledMedicationDay_Weekly_OnlyScheduledDayOfWeek()
    {
        // Arrange - Start on Monday (Nov 4, 2024 was a Monday)
        var startDate = new DateTime(2024, 11, 4); // Monday
        var medication = new Medication
        {
            Frequency = MedicationFrequency.Weekly,
            StartDate = startDate,
            IsActive = true
        };

        // Act & Assert - Test multiple weeks
        // Monday (should be scheduled)
        var monday1 = startDate;
        var method = typeof(Medication).GetMethod("IsScheduledMedicationDay",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ((bool)method!.Invoke(medication, new object[] { monday1 })!).Should().BeTrue("start date Monday is scheduled");

        // Tuesday (should not be scheduled)
        var tuesday = startDate.AddDays(1);
        ((bool)method!.Invoke(medication, new object[] { tuesday })!).Should().BeFalse("Tuesday is not scheduled");

        // Wednesday (should not be scheduled)
        var wednesday = startDate.AddDays(2);
        ((bool)method!.Invoke(medication, new object[] { wednesday })!).Should().BeFalse("Wednesday is not scheduled");

        // Next Monday (should be scheduled)
        var monday2 = startDate.AddDays(7);
        ((bool)method!.Invoke(medication, new object[] { monday2 })!).Should().BeTrue("next Monday is scheduled");

        // Monday 3 weeks later (should be scheduled)
        var monday3 = startDate.AddDays(21);
        ((bool)method!.Invoke(medication, new object[] { monday3 })!).Should().BeTrue("Monday 3 weeks later is scheduled");
    }

    [Theory]
    [InlineData(MedicationFrequency.OnceDaily)]
    [InlineData(MedicationFrequency.TwiceDaily)]
    [InlineData(MedicationFrequency.ThreeTimesDaily)]
    [InlineData(MedicationFrequency.FourTimesDaily)]
    public void IsScheduledMedicationDay_DailyFrequencies_AllDaysScheduled(MedicationFrequency frequency)
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = new Medication
        {
            Frequency = frequency,
            StartDate = startDate,
            IsActive = true
        };

        // Act & Assert - Test random days
        var method = typeof(Medication).GetMethod("IsScheduledMedicationDay",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        for (int day = 0; day < 30; day++)
        {
            var targetDate = startDate.AddDays(day);
            var result = (bool)method!.Invoke(medication, new object[] { targetDate })!;
            result.Should().BeTrue($"day {day} should be scheduled for {frequency}");
        }
    }

    [Theory]
    [InlineData(MedicationFrequency.AsNeeded)]
    [InlineData(MedicationFrequency.Custom)]
    public void IsScheduledMedicationDay_OnDemandFrequencies_AllDaysValid(MedicationFrequency frequency)
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = new Medication
        {
            Frequency = frequency,
            StartDate = startDate,
            IsActive = true
        };

        // Act & Assert - Test random days
        var method = typeof(Medication).GetMethod("IsScheduledMedicationDay",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        for (int day = 0; day < 30; day++)
        {
            var targetDate = startDate.AddDays(day);
            var result = (bool)method!.Invoke(medication, new object[] { targetDate })!;
            result.Should().BeTrue($"day {day} should be valid for {frequency} (on-demand dosing)");
        }
    }

    #endregion

    #region GetScheduledDayNumber Tests

    [Theory]
    [InlineData(0, 0)]   // Day 0 → Scheduled day 0 (0-based)
    [InlineData(2, 1)]   // Day 2 → Scheduled day 1 (0-based)
    [InlineData(4, 2)]   // Day 4 → Scheduled day 2 (0-based)
    [InlineData(10, 5)]  // Day 10 → Scheduled day 5 (0-based)
    [InlineData(20, 10)] // Day 20 → Scheduled day 10 (0-based)
    public void GetScheduledDayNumber_EveryOtherDay_ReturnsCorrectScheduledDay(int daysAfterStart, int expectedScheduledDay)
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = new Medication
        {
            Frequency = MedicationFrequency.EveryOtherDay,
            StartDate = startDate,
            IsActive = true
        };
        var targetDate = startDate.AddDays(daysAfterStart);

        // Act
        var method = typeof(Medication).GetMethod("GetScheduledDayNumber",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (int)method!.Invoke(medication, new object[] { targetDate })!;

        // Assert
        result.Should().Be(expectedScheduledDay,
            $"calendar day {daysAfterStart} should map to scheduled day {expectedScheduledDay}");
    }

    [Theory]
    [InlineData(1)]  // Day 1 (odd day) - not scheduled
    [InlineData(3)]  // Day 3 (odd day) - not scheduled
    [InlineData(5)]  // Day 5 (odd day) - not scheduled
    public void GetScheduledDayNumber_EveryOtherDay_UnscheduledDaysReturnNegative(int daysAfterStart)
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = new Medication
        {
            Frequency = MedicationFrequency.EveryOtherDay,
            StartDate = startDate,
            IsActive = true
        };
        var targetDate = startDate.AddDays(daysAfterStart);

        // Act
        var method = typeof(Medication).GetMethod("GetScheduledDayNumber",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (int)method!.Invoke(medication, new object[] { targetDate })!;

        // Assert
        result.Should().BeLessThan(0, $"unscheduled day {daysAfterStart} should return negative value");
    }

    [Fact]
    public void GetScheduledDayNumber_Weekly_OnlyScheduledDayOfWeekReturnsPositive()
    {
        // Arrange - Start on Monday (Nov 4, 2024)
        var startDate = new DateTime(2024, 11, 4); // Monday
        var medication = new Medication
        {
            Frequency = MedicationFrequency.Weekly,
            StartDate = startDate,
            IsActive = true
        };

        var method = typeof(Medication).GetMethod("GetScheduledDayNumber",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act & Assert - Monday 1 (should be scheduled day 0 - 0-based)
        var monday1 = startDate;
        var result1 = (int)method!.Invoke(medication, new object[] { monday1 })!;
        result1.Should().Be(0, "first Monday is scheduled day 0 (0-based)");

        // Tuesday (should be negative - not scheduled)
        var tuesday = startDate.AddDays(1);
        var result2 = (int)method!.Invoke(medication, new object[] { tuesday })!;
        result2.Should().BeLessThan(0, "Tuesday is not a scheduled day");

        // Monday 2 (should be scheduled day 1 - 0-based)
        var monday2 = startDate.AddDays(7);
        var result3 = (int)method!.Invoke(medication, new object[] { monday2 })!;
        result3.Should().Be(1, "second Monday is scheduled day 1 (0-based)");

        // Monday 3 (should be scheduled day 2 - 0-based)
        var monday3 = startDate.AddDays(14);
        var result4 = (int)method!.Invoke(medication, new object[] { monday3 })!;
        result4.Should().Be(2, "third Monday is scheduled day 2 (0-based)");
    }

    [Theory]
    [InlineData(MedicationFrequency.OnceDaily, 0, 0)]
    [InlineData(MedicationFrequency.OnceDaily, 5, 5)]
    [InlineData(MedicationFrequency.TwiceDaily, 0, 0)]
    [InlineData(MedicationFrequency.TwiceDaily, 10, 10)]
    [InlineData(MedicationFrequency.ThreeTimesDaily, 0, 0)]
    [InlineData(MedicationFrequency.FourTimesDaily, 7, 7)]
    public void GetScheduledDayNumber_DailyFrequencies_ReturnsSequentialDays(
        MedicationFrequency frequency, int daysAfterStart, int expectedScheduledDay)
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = new Medication
        {
            Frequency = frequency,
            StartDate = startDate,
            IsActive = true
        };
        var targetDate = startDate.AddDays(daysAfterStart);

        // Act
        var method = typeof(Medication).GetMethod("GetScheduledDayNumber",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (int)method!.Invoke(medication, new object[] { targetDate })!;

        // Assert
        result.Should().Be(expectedScheduledDay,
            $"{frequency} should map calendar day {daysAfterStart} to scheduled day {expectedScheduledDay}");
    }

    [Theory]
    [InlineData(MedicationFrequency.AsNeeded)]
    [InlineData(MedicationFrequency.Custom)]
    public void GetScheduledDayNumber_OnDemandFrequencies_ReturnsSequentialDays(MedicationFrequency frequency)
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = new Medication
        {
            Frequency = frequency,
            StartDate = startDate,
            IsActive = true
        };

        var method = typeof(Medication).GetMethod("GetScheduledDayNumber",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act & Assert - AsNeeded/Custom treat every day as sequential (0-based)
        for (int day = 0; day < 10; day++)
        {
            var targetDate = startDate.AddDays(day);
            var result = (int)method!.Invoke(medication, new object[] { targetDate })!;
            result.Should().Be(day, $"{frequency} day {day} should be scheduled day {day} (0-based)");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetScheduledDayNumber_BeforeStartDate_ReturnsNegative()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1);
        var medication = new Medication
        {
            Frequency = MedicationFrequency.OnceDaily,
            StartDate = startDate,
            IsActive = true
        };
        var targetDate = startDate.AddDays(-5); // 5 days before start

        // Act
        var method = typeof(Medication).GetMethod("GetScheduledDayNumber",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (int)method!.Invoke(medication, new object[] { targetDate })!;

        // Assert
        result.Should().BeLessThan(0, "dates before StartDate should return negative value");
    }

    [Fact]
    public void IsScheduledMedicationDay_Weekly_DifferentStartDaysOfWeek()
    {
        // Test that Weekly frequency works for any day of the week as start date
        var method = typeof(Medication).GetMethod("IsScheduledMedicationDay",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Wednesday start
        var wednesdayStart = new DateTime(2024, 11, 6); // Wednesday
        var medicationWed = new Medication
        {
            Frequency = MedicationFrequency.Weekly,
            StartDate = wednesdayStart,
            IsActive = true
        };

        // First Wednesday should be scheduled
        ((bool)method!.Invoke(medicationWed, new object[] { wednesdayStart })!)
            .Should().BeTrue("first Wednesday is scheduled");

        // Next Wednesday (7 days later) should be scheduled
        ((bool)method!.Invoke(medicationWed, new object[] { wednesdayStart.AddDays(7) })!)
            .Should().BeTrue("next Wednesday is scheduled");

        // Thursday (1 day after start) should not be scheduled
        ((bool)method!.Invoke(medicationWed, new object[] { wednesdayStart.AddDays(1) })!)
            .Should().BeFalse("Thursday is not scheduled when start is Wednesday");

        // Saturday start
        var saturdayStart = new DateTime(2024, 11, 9); // Saturday
        var medicationSat = new Medication
        {
            Frequency = MedicationFrequency.Weekly,
            StartDate = saturdayStart,
            IsActive = true
        };

        // First Saturday should be scheduled
        ((bool)method!.Invoke(medicationSat, new object[] { saturdayStart })!)
            .Should().BeTrue("first Saturday is scheduled");

        // Next Saturday should be scheduled
        ((bool)method!.Invoke(medicationSat, new object[] { saturdayStart.AddDays(7) })!)
            .Should().BeTrue("next Saturday is scheduled");

        // Sunday should not be scheduled
        ((bool)method!.Invoke(medicationSat, new object[] { saturdayStart.AddDays(1) })!)
            .Should().BeFalse("Sunday is not scheduled when start is Saturday");
    }

    #endregion
}
