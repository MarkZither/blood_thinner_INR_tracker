using BloodThinnerTracker.Mobile.Services;
using Xunit;

namespace Mobile.UnitTests.Services;

/// <summary>
/// Tests for SchedulingManager - the platform-agnostic scheduling logic.
/// These tests verify idempotent scheduling behavior without requiring Android.
/// </summary>
public class SchedulingManagerTests
{
    #region ShouldSchedule Tests

    [Fact]
    public void ShouldSchedule_WhenNotScheduledAndForceIsFalse_ReturnsTrue()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();
        store.SetScheduled(false);

        // Act
        var result = SchedulingManager.ShouldSchedule(store, force: false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldSchedule_WhenAlreadyScheduledAndForceIsFalse_ReturnsFalse()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();
        store.SetScheduled(true);

        // Act
        var result = SchedulingManager.ShouldSchedule(store, force: false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldSchedule_WhenAlreadyScheduledAndForceIsTrue_ReturnsTrue()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();
        store.SetScheduled(true);

        // Act
        var result = SchedulingManager.ShouldSchedule(store, force: true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldSchedule_WhenStoreIsNull_ReturnsTrue()
    {
        // Act - passing null store should allow scheduling (conservative default)
        var result = SchedulingManager.ShouldSchedule(null!, force: false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldSchedule_WhenNotScheduledAndForceIsTrue_ReturnsTrue()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();
        store.SetScheduled(false);

        // Act
        var result = SchedulingManager.ShouldSchedule(store, force: true);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region MarkScheduled Tests

    [Fact]
    public void MarkScheduled_SetsScheduledToTrue()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();
        Assert.False(store.IsScheduled());

        // Act
        SchedulingManager.MarkScheduled(store);

        // Assert
        Assert.True(store.IsScheduled());
    }

    [Fact]
    public void MarkScheduled_WhenStoreIsNull_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var exception = Record.Exception(() => SchedulingManager.MarkScheduled(null!));
        Assert.Null(exception);
    }

    #endregion

    #region ClearScheduled Tests

    [Fact]
    public void ClearScheduled_SetsScheduledToFalse()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();
        store.SetScheduled(true);
        Assert.True(store.IsScheduled());

        // Act
        SchedulingManager.ClearScheduled(store);

        // Assert
        Assert.False(store.IsScheduled());
    }

    [Fact]
    public void ClearScheduled_WhenStoreIsNull_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var exception = Record.Exception(() => SchedulingManager.ClearScheduled(null!));
        Assert.Null(exception);
    }

    #endregion

    #region Integration-style Tests

    [Fact]
    public void SchedulingFlow_FirstSchedule_ThenBlockSubsequent_ThenForceReschedule()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();

        // Act & Assert - First schedule should be allowed
        Assert.True(SchedulingManager.ShouldSchedule(store, force: false));
        SchedulingManager.MarkScheduled(store);

        // Subsequent schedule attempts should be blocked
        Assert.False(SchedulingManager.ShouldSchedule(store, force: false));
        Assert.False(SchedulingManager.ShouldSchedule(store, force: false));

        // Force should override
        Assert.True(SchedulingManager.ShouldSchedule(store, force: true));
    }

    [Fact]
    public void SchedulingFlow_CancelThenReschedule()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();
        SchedulingManager.MarkScheduled(store);
        Assert.False(SchedulingManager.ShouldSchedule(store, force: false));

        // Act - cancel clears the flag
        SchedulingManager.ClearScheduled(store);

        // Assert - should now allow scheduling again
        Assert.True(SchedulingManager.ShouldSchedule(store, force: false));
    }

    #endregion
}
