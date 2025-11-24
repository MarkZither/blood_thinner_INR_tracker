using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Mobile.ViewModels;
using Moq;
using Xunit;

namespace BloodThinnerTracker.Mobile.UnitTests;

/// <summary>
/// Unit tests for InrListViewModel.
/// Tests cache integration, stale detection, offline fallback, and load scenarios.
/// </summary>
public class InrListViewModelTests
{
    private readonly MockInrService _mockInrService = new();
    private readonly MockCacheService _mockCacheService = new();
    private readonly InrListViewModel _viewModel;

    public InrListViewModelTests()
    {
        _viewModel = new InrListViewModel(_mockInrService, _mockCacheService);
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
    public async Task LoadInrLogs_CachesData_AfterFetch()
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
        Assert.NotNull(_mockCacheService.LastCachedData);
        var cached = JsonSerializer.Deserialize<List<InrListItemVm>>(_mockCacheService.LastCachedData);
        Assert.NotNull(cached);
        Assert.Single(cached);
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
    public async Task LoadInrLogs_FallsBackToCache_WhenNetworkFails()
    {
        // Arrange
        var cachedData = new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now.AddDays(-5), InrValue = 2.3m }
        };
        _mockCacheService.SetCachedData(JsonSerializer.Serialize(cachedData), ageHours: 2);
        _mockInrService.ThrowException = true;

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.ShowList);
        Assert.Single(_viewModel.InrLogs);
        Assert.True(_viewModel.IsOfflineMode);
        Assert.True(_viewModel.ShowStaleWarning);
    }

    [Fact]
    public async Task LoadInrLogs_ShowsStaleWarning_WhenCacheAgeExceeds1Hour()
    {
        // Arrange
        var cachedData = new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 2.5m }
        };
        _mockCacheService.SetCachedData(JsonSerializer.Serialize(cachedData), ageHours: 3);

        // Act
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.ShowStaleWarning);
        Assert.Contains("3.0", _viewModel.StaleWarningText);
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
    public async Task LoadInrLogs_ClearsStaleWarnings_OnFreshLoad()
    {
        // Arrange - first load with old cache
        var cachedData = new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now.AddDays(-5), InrValue = 2.3m }
        };
        _mockCacheService.SetCachedData(JsonSerializer.Serialize(cachedData), ageHours: 5);
        await _viewModel.LoadInrLogsCommand.ExecuteAsync(null);
        Assert.True(_viewModel.ShowStaleWarning);

        // Now have fresh data available
        var freshData = new List<InrListItemVm>
        {
            new() { PublicId = Guid.NewGuid(), TestDate = DateTime.Now, InrValue = 2.5m }
        };
        _mockInrService.SetTestData(freshData);
        _mockCacheService.ClearCache();

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
    /// Mock cache service for testing.
    /// </summary>
    private class MockCacheService : ICacheService
    {
        private string? _cachedData;
        private DateTime _cachedAt = DateTime.MinValue;
        public string? LastCachedData { get; private set; }

        public void SetCachedData(string data, double ageHours = 0)
        {
            _cachedData = data;
            _cachedAt = DateTime.UtcNow.AddHours(-ageHours);
        }

        public void ClearCache()
        {
            _cachedData = null;
            _cachedAt = DateTime.MinValue;
        }

        public Task<string?> GetAsync(string key) => Task.FromResult(_cachedData);

        public Task SetAsync(string key, string jsonPayload, TimeSpan? expiresIn = null)
        {
            LastCachedData = jsonPayload;
            _cachedData = jsonPayload;
            _cachedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task<bool> HasValidCacheAsync(string key) => Task.FromResult(_cachedData != null);

        public Task<long?> GetCacheAgeMillisecondsAsync(string key)
        {
            if (_cachedAt == DateTime.MinValue)
                return Task.FromResult<long?>(null);

            var age = DateTime.UtcNow - _cachedAt;
            return Task.FromResult<long?>((long)age.TotalMilliseconds);
        }

        public Task ClearAsync(string key)
        {
            _cachedData = null;
            return Task.CompletedTask;
        }

        public Task<DateTime?> GetExpirationTimeAsync(string key) => Task.FromResult<DateTime?>(DateTime.UtcNow.AddDays(7));
    }
}
