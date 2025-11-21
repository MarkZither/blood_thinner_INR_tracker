using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace BloodThinnerTracker.Mobile.Services
{
    public class SecureStorageService : ISecureStorageService
    {
        public async Task SetAsync(string key, string value)
        {
            try
            {
                await SecureStorage.SetAsync(key, value);
            }
            catch (Exception)
            {
                // Best-effort; consumer should handle failures
            }
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                return await SecureStorage.GetAsync(key);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task RemoveAsync(string key)
        {
            try
            {
                SecureStorage.Remove(key);
            }
            catch (Exception)
            {
                // ignore
            }

            return Task.CompletedTask;
        }
    }
}
