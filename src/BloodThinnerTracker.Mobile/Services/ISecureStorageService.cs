using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Services
{
    public interface ISecureStorageService
    {
        Task SetAsync(string key, string value);
        Task<string?> GetAsync(string key);
        Task RemoveAsync(string key);
        /// <summary>
        /// Try to get a value from secure storage without throwing on failure.
        /// Returns a tuple: (success, value). On failure, success=false and value=null.
        /// </summary>
        Task<(bool success, string? value)> TryGetAsync(string key);

        /// <summary>
        /// Try to remove a key from secure storage without throwing on failure.
        /// Returns true if removal succeeded, false otherwise.
        /// </summary>
        Task<bool> TryRemoveAsync(string key);
    }
}
