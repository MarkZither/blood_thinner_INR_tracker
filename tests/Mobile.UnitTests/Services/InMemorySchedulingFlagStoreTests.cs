using BloodThinnerTracker.Mobile.Services;
using Xunit;

namespace Mobile.UnitTests.Services;

/// <summary>
/// Tests for InMemorySchedulingFlagStore - the test-friendly implementation of ISchedulingFlagStore.
/// </summary>
public class InMemorySchedulingFlagStoreTests
{
    [Fact]
    public void IsScheduled_DefaultsToFalse()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();

        // Act & Assert
        Assert.False(store.IsScheduled());
    }

    [Fact]
    public void SetScheduled_True_SetsFlag()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();

        // Act
        store.SetScheduled(true);

        // Assert
        Assert.True(store.IsScheduled());
    }

    [Fact]
    public void SetScheduled_False_ClearsFlag()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();
        store.SetScheduled(true);

        // Act
        store.SetScheduled(false);

        // Assert
        Assert.False(store.IsScheduled());
    }

    [Fact]
    public void SetScheduled_MultipleTimes_LastValueWins()
    {
        // Arrange
        var store = new InMemorySchedulingFlagStore();

        // Act
        store.SetScheduled(true);
        store.SetScheduled(false);
        store.SetScheduled(true);

        // Assert
        Assert.True(store.IsScheduled());
    }

    [Fact]
    public void ImplementsInterface()
    {
        // Arrange & Act
        ISchedulingFlagStore store = new InMemorySchedulingFlagStore();

        // Assert - verify it implements the interface correctly
        store.SetScheduled(true);
        Assert.True(store.IsScheduled());
    }
}
