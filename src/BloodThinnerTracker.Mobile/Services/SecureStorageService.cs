using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace BloodThinnerTracker.Mobile.Services
{
    public class SecureStorageService : ISecureStorageService
    {
        private readonly ILogger<SecureStorageService> _logger;

        public SecureStorageService(ILogger<SecureStorageService> logger)
        {
            _logger = logger;
        }

        public async Task SetAsync(string key, string value)
        {
            try
            {
                await SecureStorage.SetAsync(key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecureStorage.SetAsync failed for key {Key}", key);
                throw;
            }
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                return await SecureStorage.GetAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecureStorage.GetAsync failed for key {Key}", key);
                throw;
            }
        }

        public Task RemoveAsync(string key)
        {
            try
            {
                SecureStorage.Remove(key);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecureStorage.Remove failed for key {Key}", key);
                throw;
            }
        }

        public async Task<(bool success, string? value)> TryGetAsync(string key)
        {
            try
            {
                var v = await SecureStorage.GetAsync(key);
                return (v != null, v);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SecureStorage.TryGetAsync failed for key {Key}", key);
                return (false, null);
            }
        }

        public Task<bool> TryRemoveAsync(string key)
        {
            try
            {
                SecureStorage.Remove(key);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SecureStorage.TryRemoveAsync failed for key {Key}", key);
                return Task.FromResult(false);
            }
        }
    }
}
