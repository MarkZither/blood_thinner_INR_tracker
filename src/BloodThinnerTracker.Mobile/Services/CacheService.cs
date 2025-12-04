using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Cache metadata for tracking staleness and expiration.
    /// </summary>
    public class CacheMetadata
    {
        public DateTime CachedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Encrypted cache entry combining encrypted payload and metadata.
    /// </summary>
    public class CacheEntry
    {
        public EncryptedPayload EncryptedPayload { get; set; } = new();
        public CacheMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Service for managing encrypted, expiring cache of INR data.
    ///
    /// Features:
    /// - AES-256 encryption for health data (Constitution requirement)
    /// - Configurable expiration (default 7 days)
    /// - Staleness detection (warn after 1 hour)
    /// - Secure storage using platform SecureStorage
    /// - Graceful fallback for offline access
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get cached data if available and not expired.
        /// </summary>
        /// <param name="key">Cache key (e.g., "inr_logs")</param>
        /// <returns>Cached JSON payload or null if expired/missing</returns>
        Task<string?> GetAsync(string key);

        /// <summary>
        /// Store data with automatic encryption and expiration.
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="jsonPayload">JSON data to cache</param>
        /// <param name="expiresIn">How long to keep cache (default 7 days)</param>
        Task SetAsync(string key, string jsonPayload, TimeSpan? expiresIn = null);

        /// <summary>
        /// Check if cache entry exists and is not expired.
        /// </summary>
        Task<bool> HasValidCacheAsync(string key);

        /// <summary>
        /// Get age of cache entry in milliseconds.
        /// </summary>
        /// <returns>Milliseconds since cached, or null if not found</returns>
        Task<long?> GetCacheAgeMillisecondsAsync(string key);

        /// <summary>
        /// Clear cache entry.
        /// </summary>
        Task ClearAsync(string key);

        /// <summary>
        /// Get expiration time for cache entry.
        /// </summary>
        /// <returns>DateTime when cache expires, or null if not found</returns>
        Task<DateTime?> GetExpirationTimeAsync(string key);
    }

    public class CacheService : ICacheService
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly EncryptionService _encryptionService;
        private readonly ILogger<CacheService>? _logger;
        private byte[]? _cacheKey;

        // Default cache retention: 7 days
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromDays(7);

        // Key for storing encryption key in secure storage
        private const string CacheKeyStorageKey = "cache_encryption_key";

        public CacheService(ISecureStorageService secureStorage, EncryptionService encryptionService, ILogger<CacheService>? logger = null)
        {
            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _logger = logger;
        }

        /// <summary>
        /// Get or create the cache encryption key.
        /// Key is generated once per device and stored in platform secure storage.
        /// </summary>
        private async Task<byte[]> GetOrCreateCacheKeyAsync()
        {
            if (_cacheKey != null)
                return _cacheKey;

            // Try to retrieve existing key from secure storage
            var storedKey = await _secureStorage.GetAsync(CacheKeyStorageKey);
            if (!string.IsNullOrEmpty(storedKey))
            {
                _cacheKey = Convert.FromBase64String(storedKey);
                return _cacheKey;
            }

            // Generate new key if not found
            _cacheKey = _encryptionService.GenerateRandomKey();
            var keyBase64 = Convert.ToBase64String(_cacheKey);
            await _secureStorage.SetAsync(CacheKeyStorageKey, keyBase64);

            return _cacheKey;
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                // Get encrypted cache entry from secure storage
                var cachedJson = await _secureStorage.GetAsync($"cache_{key}");
                if (string.IsNullOrEmpty(cachedJson))
                    return null;

                // Deserialize cache entry
                var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(cachedJson);
                if (cacheEntry?.EncryptedPayload == null)
                    return null;

                // Check expiration
                if (cacheEntry.Metadata.ExpiresAt < DateTime.UtcNow)
                {
                    // Cache expired - remove it
                    await ClearAsync(key);
                    return null;
                }

                // Decrypt and return payload
                var cacheKey = await GetOrCreateCacheKeyAsync();
                var plaintext = _encryptionService.Decrypt(cacheKey, cacheEntry.EncryptedPayload);
                return plaintext;
            }
            catch (Exception ex)
            {
                // Log error but don't throw - cache failure should not crash app
                _logger?.LogError(ex, "CacheService.GetAsync error fetching cache for key {Key}", key);
                return null;
            }
        }

        public async Task SetAsync(string key, string jsonPayload, TimeSpan? expiresIn = null)
        {
            try
            {
                var cacheKey = await GetOrCreateCacheKeyAsync();
                var expirationDuration = expiresIn ?? DefaultCacheDuration;

                // Encrypt payload
                var encrypted = _encryptionService.Encrypt(cacheKey, jsonPayload);

                // Create cache entry with metadata
                var now = DateTime.UtcNow;
                var cacheEntry = new CacheEntry
                {
                    EncryptedPayload = encrypted,
                    Metadata = new CacheMetadata
                    {
                        CachedAt = now,
                        ExpiresAt = now.Add(expirationDuration)
                    }
                };

                // Store in secure storage
                var cacheJson = JsonSerializer.Serialize(cacheEntry);
                await _secureStorage.SetAsync($"cache_{key}", cacheJson);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - cache failure should not crash app
                _logger?.LogError(ex, "CacheService.SetAsync error storing cache for key {Key}", key);
            }
        }

        public async Task<bool> HasValidCacheAsync(string key)
        {
            var cached = await GetAsync(key);
            return cached != null;
        }

        public async Task<long?> GetCacheAgeMillisecondsAsync(string key)
        {
            try
            {
                var cachedJson = await _secureStorage.GetAsync($"cache_{key}");
                if (string.IsNullOrEmpty(cachedJson))
                    return null;

                var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(cachedJson);
                if (cacheEntry?.Metadata == null)
                    return null;

                var age = DateTime.UtcNow - cacheEntry.Metadata.CachedAt;
                return (long)age.TotalMilliseconds;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CacheService.GetCacheAgeMillisecondsAsync error for key {Key}", key);
                return null;
            }
        }

        public async Task ClearAsync(string key)
        {
            try
            {
                await _secureStorage.RemoveAsync($"cache_{key}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CacheService.ClearAsync error for key {Key}", key);
            }
        }

        public async Task<DateTime?> GetExpirationTimeAsync(string key)
        {
            try
            {
                var cachedJson = await _secureStorage.GetAsync($"cache_{key}");
                if (string.IsNullOrEmpty(cachedJson))
                    return null;

                var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(cachedJson);
                return cacheEntry?.Metadata?.ExpiresAt;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CacheService.GetExpirationTimeAsync error for key {Key}", key);
                return null;
            }
        }
    }
}
