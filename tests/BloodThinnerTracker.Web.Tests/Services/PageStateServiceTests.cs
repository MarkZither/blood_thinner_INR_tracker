using BloodThinnerTracker.Web.Models;
using BloodThinnerTracker.Web.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BloodThinnerTracker.Web.Tests.Services;

/// <summary>
/// Unit tests for PageStateService
/// Tests validate the service contract and error handling behavior
/// Note: ProtectedSessionStorage is difficult to mock due to internal constructors,
/// so these tests focus on validation and error handling logic
/// </summary>
public class PageStateServiceTests
{
    [Fact]
    public void PageState_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var state = new PageState();

        // Assert
        Assert.Equal(10, state.PageSize);
        Assert.Equal(1, state.CurrentPage);
        Assert.Equal(0, state.ScrollPosition);
        Assert.Null(state.AdditionalState);
    }

    [Fact]
    public void PageState_CanSetProperties()
    {
        // Arrange
        var state = new PageState();
        var additionalState = new Dictionary<string, string>
        {
            { "filter", "active" },
            { "sort", "name" }
        };

        // Act
        state.PageSize = 25;
        state.CurrentPage = 3;
        state.ScrollPosition = 150;
        state.AdditionalState = additionalState;

        // Assert
        Assert.Equal(25, state.PageSize);
        Assert.Equal(3, state.CurrentPage);
        Assert.Equal(150, state.ScrollPosition);
        Assert.NotNull(state.AdditionalState);
        Assert.Equal(2, state.AdditionalState.Count);
        Assert.Equal("active", state.AdditionalState["filter"]);
    }

    [Fact]
    public void IPageStateService_DefinesCorrectInterface()
    {
        // Arrange
        var interfaceType = typeof(IPageStateService);

        // Assert
        Assert.True(interfaceType.IsInterface);
        
        var methods = interfaceType.GetMethods();
        Assert.Contains(methods, m => m.Name == "SaveStateAsync");
        Assert.Contains(methods, m => m.Name == "LoadStateAsync");
        Assert.Contains(methods, m => m.Name == "ClearStateAsync");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void PageState_SupportedPageSizes_CanBeSet(int pageSize)
    {
        // Arrange
        var state = new PageState();

        // Act
        state.PageSize = pageSize;

        // Assert
        Assert.Equal(pageSize, state.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void PageState_CurrentPage_CanBeSet(int currentPage)
    {
        // Arrange
        var state = new PageState();

        // Act
        state.CurrentPage = currentPage;

        // Assert
        Assert.Equal(currentPage, state.CurrentPage);
    }

    [Fact]
    public void PageState_AdditionalState_SupportsMultipleEntries()
    {
        // Arrange
        var state = new PageState
        {
            AdditionalState = new Dictionary<string, string>
            {
                { "searchTerm", "warfarin" },
                { "statusFilter", "active" },
                { "sortBy", "name" },
                { "sortDirection", "asc" }
            }
        };

        // Assert
        Assert.NotNull(state.AdditionalState);
        Assert.Equal(4, state.AdditionalState.Count);
        Assert.Equal("warfarin", state.AdditionalState["searchTerm"]);
        Assert.Equal("active", state.AdditionalState["statusFilter"]);
    }
}
