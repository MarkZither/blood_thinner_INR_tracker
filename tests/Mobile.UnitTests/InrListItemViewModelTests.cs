using System;
using BloodThinnerTracker.Mobile.ViewModels;
using Xunit;

namespace BloodThinnerTracker.Mobile.UnitTests;

/// <summary>
/// Unit tests for InrListItemViewModel.
/// Tests status indicators, color coding, and display properties.
/// </summary>
public class InrListItemViewModelTests
{
    [Fact]
    public void StatusLabel_IsNormal_WhenInrInRange()
    {
        // Arrange
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = 2.5m,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var label = viewModel.StatusLabel;

        // Assert
        Assert.Equal("NORMAL", label);
    }

    [Fact]
    public void StatusLabel_IsElevated_WhenInrAbove3()
    {
        // Arrange
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = 3.5m,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var label = viewModel.StatusLabel;

        // Assert
        Assert.Equal("ELEVATED", label);
    }

    [Fact]
    public void StatusLabel_IsLow_WhenInrBelow2()
    {
        // Arrange
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = 1.5m,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var label = viewModel.StatusLabel;

        // Assert
        Assert.Equal("LOW", label);
    }

    [Fact]
    public void StatusColor_IsGreen_WhenNormal()
    {
        // Arrange
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = 2.5m,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var color = viewModel.StatusColor;

        // Assert - Green for normal
        Assert.Equal(Color.FromArgb("#28A745"), color);
    }

    [Fact]
    public void StatusColor_IsOrange_WhenElevated()
    {
        // Arrange
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = 3.5m,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var color = viewModel.StatusColor;

        // Assert - Orange for elevated
        Assert.Equal(Color.FromArgb("#FFC107"), color);
    }

    [Fact]
    public void StatusColor_IsRed_WhenLow()
    {
        // Arrange
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = 1.5m,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var color = viewModel.StatusColor;

        // Assert - Red for low
        Assert.Equal(Color.FromArgb("#DC3545"), color);
    }

    [Fact]
    public void InrValueColor_IsGreen_WhenNormal()
    {
        // Arrange
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = 2.5m,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var color = viewModel.InrValueColor;

        // Assert - Green for normal
        Assert.Equal(Color.FromArgb("#28A745"), color);
    }

    [Fact]
    public void InrValueColor_IsRed_WhenOutOfRange()
    {
        // Arrange - elevated case
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = 3.5m,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var color = viewModel.InrValueColor;

        // Assert - Red for out-of-range
        Assert.Equal(Color.FromArgb("#DC3545"), color);
    }

    [Fact]
    public void InrValueColor_IsRed_WhenLow()
    {
        // Arrange - low case
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = 1.5m,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var color = viewModel.InrValueColor;

        // Assert - Red for out-of-range
        Assert.Equal(Color.FromArgb("#DC3545"), color);
    }

    [Theory]
    [InlineData(2.0)]
    [InlineData(3.0)]
    [InlineData(2.5)]
    public void StatusLabel_IsNormal_ForBoundaryValues(double inrValue)
    {
        // Arrange
        var model = new InrListItemVm
        {
            PublicId = Guid.NewGuid(),
            TestDate = DateTime.Now,
            InrValue = (decimal)inrValue,
            Notes = ""
        };
        var viewModel = new InrListItemViewModel(model);

        // Act
        var label = viewModel.StatusLabel;

        // Assert
        Assert.Equal("NORMAL", label);
    }

    [Fact]
    public void InrListItemViewModel_InitializesWithModel()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var testDate = new DateTime(2025, 11, 22);
        var model = new InrListItemVm
        {
            PublicId = testId,
            TestDate = testDate,
            InrValue = 2.5m,
            Notes = "Test note",
            ReviewedByProvider = true
        };

        // Act
        var viewModel = new InrListItemViewModel(model);

        // Assert
        Assert.Equal(testId, viewModel.PublicId);
        Assert.Equal(testDate, viewModel.TestDate);
        Assert.Equal(2.5m, viewModel.InrValue);
        Assert.Equal("Test note", viewModel.Notes);
        Assert.True(viewModel.ReviewedByProvider);
    }

    [Fact]
    public void InrListItemViewModel_ThrowsOnNullModel()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new InrListItemViewModel(null!));
        Assert.Equal("model", ex.ParamName);
    }
}
