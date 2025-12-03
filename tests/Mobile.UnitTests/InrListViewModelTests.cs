using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Mobile.ViewModels;
using Xunit;

namespace BloodThinnerTracker.Mobile.UnitTests;

/// <summary>
/// Unit tests for InrListViewModel.
/// Tests cache integration, stale detection, offline fallback, and load scenarios.
/// </summary>
public class InrListViewModelTests
{
    private readonly MockInrService _mockInrService = new();
    private readonly MockInrRepository _mockInrRepository = new();
    private readonly InrListViewModel _viewModel;

    public InrListViewModelTests()
    {
        _viewModel = new InrListViewModel(_mockInrService, _mockInrRepository);
    }

    [Fact]
    public async Task LoadInrLogs_DisplaysData_WhenAvailable()
    {
        // Arrange
        var testData = new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 2.5m, Notes = "Test 1" },
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now.AddDays(-1), InrValue = 2.8m, Notes = "Test 2" }
        };
        _mockInrService.SetTestData(testData);

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.ShowList);
        Assert.Equal(2, _viewModel.InrLogs.Count);
        Assert.False(_viewModel.ShowError);
        Assert.False(_viewModel.ShowEmpty);
    }

    [Fact]
    public async Task LoadInrLogs_UsesLocalRepository_WhenAvailable()
    {
        // Arrange - no data in service, but data in local repository
        _mockInrService.SetTestData(new List<InrListItemVm>());
        await _mockInrRepository.SaveRangeAsync(new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 2.5m }
        });

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert - should use local repository data
        Assert.True(_viewModel.ShowList);
        Assert.Single(_viewModel.InrLogs);
    }

    [Fact]
    public async Task LoadInrLogs_ShowsEmptyState_WhenNoData()
    {
        // Arrange
        _mockInrService.SetTestData(new List<InrListItemVm>());

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.ShowEmpty);
        Assert.False(_viewModel.ShowList);
        Assert.False(_viewModel.ShowError);
    }

    [Fact]
    public async Task LoadInrLogs_ShowsError_WhenFetchFails()
    {
        // Arrange
        _mockInrService.ThrowException = true;

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.ShowError);
        Assert.NotNull(_viewModel.ErrorMessage);
        Assert.Contains("Failed to fetch", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadInrLogs_FallsBackToLocalData_WhenNetworkFails()
    {
        // Arrange - data in local repository, network fails
        await _mockInrRepository.SaveRangeAsync(new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now.AddDays(-5), InrValue = 2.3m }
        });
        _mockInrService.ThrowException = true;

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert - should use local data
        Assert.True(_viewModel.ShowList);
        Assert.Single(_viewModel.InrLogs);
    }

    [Fact]
    public async Task LoadInrLogs_PrefersLocalData_OverRemote()
    {
        // Arrange - data in both local and remote
        await _mockInrRepository.SaveRangeAsync(new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 2.5m, Notes = "Local" }
        });
        _mockInrService.SetTestData(new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 3.0m, Notes = "Remote" }
        });

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert - should use local data (offline-first)
        Assert.True(_viewModel.ShowList);
        Assert.Single(_viewModel.InrLogs);
        Assert.Equal(2.5m, _viewModel.InrLogs[0].InrValue);
    }

    [Fact]
    public async Task LoadInrLogs_DoesNotShowStaleWarning_WhenCacheIsFresh()
    {
        // Arrange
        var testData = new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 2.5m }
        };
        _mockInrService.SetTestData(testData);

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_viewModel.ShowStaleWarning);
        Assert.False(_viewModel.IsOfflineMode);
    }

    [Fact]
    public async Task LoadInrLogs_DisablesRefreshButton_WhileLoading()
    {
        // Arrange
        _mockInrService.SetSimulatedLatency(500);
        var testData = new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 2.5m }
        };
        _mockInrService.SetTestData(testData);

        // Act
        var loadTask = _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert - should be busy immediately
        Assert.True(_viewModel.IsBusy);

        await loadTask;

        // Assert - should not be busy after load
        Assert.False(_viewModel.IsBusy);
    }

    [Fact]
    public async Task LoadInrLogs_UpdatesLastUpdatedText()
    {
        // Arrange
        var testData = new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 2.5m }
        };
        _mockInrService.SetTestData(testData);

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert
        Assert.NotEqual("Never updated", _viewModel.LastUpdatedText);
        Assert.Contains("Updated", _viewModel.LastUpdatedText);
    }

    [Fact]
    public async Task LoadInrLogs_ShowsData_OnSuccessfulLoad()
    {
        // Arrange - fresh data available from service
        var freshData = new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 2.5m }
        };
        _mockInrService.SetTestData(freshData);

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_viewModel.ShowStaleWarning);
        Assert.False(_viewModel.IsOfflineMode);
        Assert.True(_viewModel.ShowList);
    }

    /// <summary>
    /// Mock INR service for testing.
    /// </summary>
    private class MockInrService : IInrService
    {
        private List<InrListItemVm> _testData = new();
        public bool ThrowException { get; set; }
        public int SimulatedLatency { get; set; }

        public void SetTestData(List<InrListItemVm> data) => _testData = data;
        public void SetSimulatedLatency(int ms) => SimulatedLatency = ms;

        public async Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count)
        {
            if (SimulatedLatency > 0)
                await Task.Delay(SimulatedLatency);

            if (ThrowException)
                throw new HttpRequestException("Network error");

            return _testData.Take(count).ToList();
        }
    }

    /// <summary>
    /// Mock INR repository for testing.
    /// </summary>
    private class MockInrRepository : IInrRepository
    {
        private readonly List<InrListItemVm> _data = new();

        public Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 10)
        {
            return Task.FromResult<IEnumerable<InrListItemVm>>(_data.Take(count).ToList());
        }

        public Task SaveRangeAsync(IEnumerable<InrListItemVm> items)
        {
            _data.AddRange(items);
            return Task.CompletedTask;
        }
    }
}
