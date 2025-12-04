using System;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.Services;
using Xunit;

namespace BloodThinnerTracker.Mobile.UnitTests;

/// <summary>
/// Unit tests for CacheService.
/// Tests encryption, expiration, staleness detection, and offline fallback.
/// </summary>
public class CacheServiceTests
{
    private readonly MockSecureStorageService _mockStorage = new();
    private readonly EncryptionService _encryptionService = new();
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _cacheService = new CacheService(_mockStorage, _encryptionService);
    }

    [Fact]
    public async Task SetAsync_StoresEncryptedData()
    {
        // Arrange
        var key = "test_key";
        var payload = """{"id": 1, "value": 2.5}""";

        // Act
        await _cacheService.SetAsync(key, payload);

        // Assert
        var stored = await _mockStorage.GetAsync($"cache_{key}");
        Assert.NotNull(stored);
        Assert.Contains("EncryptedPayload", stored);
        Assert.Contains("Metadata", stored);
    }

    [Fact]
    public async Task GetAsync_ReturnsOriginalData_WhenNotExpired()
    {
        // Arrange
        var key = "test_key";
        var payload = """{"id": 1, "value": 2.5}""";
        await _cacheService.SetAsync(key, payload);

        // Act
        var retrieved = await _cacheService.GetAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(payload, retrieved);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenExpired()
    {
        // Arrange
        var key = "test_key";
        var payload = """{"id": 1, "value": 2.5}""";

        // Set cache with 1-second expiration
        await _cacheService.SetAsync(key, payload, expiresIn: TimeSpan.FromSeconds(1));

        // Act - wait for expiration
        await Task.Delay(1100);
        var retrieved = await _cacheService.GetAsync(key);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task HasValidCacheAsync_ReturnsTrue_WhenCacheExists()
    {
        // Arrange
        var key = "test_key";
        var payload = """{"id": 1}""";
        await _cacheService.SetAsync(key, payload);

        // Act
        var hasValid = await _cacheService.HasValidCacheAsync(key);

        // Assert
        Assert.True(hasValid);
    }

    [Fact]
    public async Task HasValidCacheAsync_ReturnsFalse_WhenCacheMissing()
    {
        // Act
        var hasValid = await _cacheService.HasValidCacheAsync("nonexistent");

        // Assert
        Assert.False(hasValid);
    }

    [Fact]
    public async Task GetCacheAgeMillisecondsAsync_ReturnsApproximateAge()
    {
        // Arrange
        var key = "test_key";
        var payload = """{"id": 1}""";
        await _cacheService.SetAsync(key, payload);

        // Act
        await Task.Delay(100);
        var ageMs = await _cacheService.GetCacheAgeMillisecondsAsync(key);

        // Assert
        Assert.NotNull(ageMs);
        Assert.True(ageMs >= 100, "Cache age should be at least 100ms");
        Assert.True(ageMs < 500, "Cache age should be less than 500ms");
    }

    [Fact]
    public async Task GetCacheAgeMillisecondsAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var ageMs = await _cacheService.GetCacheAgeMillisecondsAsync("nonexistent");

        // Assert
        Assert.Null(ageMs);
    }

    [Fact]
    public async Task ClearAsync_RemovesCache()
    {
        // Arrange
        var key = "test_key";
        var payload = """{"id": 1}""";
        await _cacheService.SetAsync(key, payload);

        // Act
        await _cacheService.ClearAsync(key);
        var retrieved = await _cacheService.GetAsync(key);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetExpirationTimeAsync_ReturnsExactTime()
    {
        // Arrange
        var key = "test_key";
        var payload = """{"id": 1}""";
        var duration = TimeSpan.FromHours(1);
        await _cacheService.SetAsync(key, payload, expiresIn: duration);

        var beforeCheck = DateTime.UtcNow.Add(duration);

        // Act
        var expirationTime = await _cacheService.GetExpirationTimeAsync(key);

        // Assert
        Assert.NotNull(expirationTime);
        Assert.True(expirationTime > DateTime.UtcNow);
        Assert.True(expirationTime <= beforeCheck.AddSeconds(1));
    }

    [Fact]
    public async Task MultipleEntries_AreIndependent()
    {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var payload1 = """{"id": 1}""";
        var payload2 = """{"id": 2}""";

        // Act
        await _cacheService.SetAsync(key1, payload1);
        await _cacheService.SetAsync(key2, payload2);
        await _cacheService.ClearAsync(key1);

        // Assert
        Assert.Null(await _cacheService.GetAsync(key1));
        Assert.NotNull(await _cacheService.GetAsync(key2));
    }

    [Fact]
    public async Task EncryptionKey_IsPersisted()
    {
        // Arrange
        var key = "test_key";
        var payload = """{"id": 1}""";
        await _cacheService.SetAsync(key, payload);

        // Create new cache service instance (same storage backend)
        var newCacheService = new CacheService(_mockStorage, _encryptionService);

        // Act
        var retrieved = await newCacheService.GetAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(payload, retrieved);
    }

    /// <summary>
    /// Mock implementation of ISecureStorageService for testing.
    /// </summary>
    private class MockSecureStorageService : ISecureStorageService
    {
        private readonly Dictionary<string, string> _storage = new();

        public Task SetAsync(string key, string value)
        {
            _storage[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
        {
            var value = _storage.TryGetValue(key, out var result) ? result : null;
            return Task.FromResult(value);
        }

        public Task RemoveAsync(string key)
        {
            _storage.Remove(key);
            return Task.CompletedTask;
        }
        public Task<(bool success, string? value)> TryGetAsync(string key)
        {
            var ok = _storage.TryGetValue(key, out var v);
            return Task.FromResult((ok, ok ? v : null));
        }

        public Task<bool> TryRemoveAsync(string key)
        {
            var removed = _storage.Remove(key);
            return Task.FromResult(removed);
        }
    }
}
