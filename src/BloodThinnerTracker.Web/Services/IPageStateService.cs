namespace BloodThinnerTracker.Web.Services;

/// <summary>
/// Service for persisting page state across navigation in both SSR and WASM modes.
/// Uses browser storage to remember pagination, filters, and scroll position.
/// </summary>
public interface IPageStateService
{
    /// <summary>
    /// Save page state for a specific page key
    /// </summary>
    /// <param name="pageKey">Unique key for the page (e.g., "inr-list")</param>
    /// <param name="state">The state object to save</param>
    /// <returns>Task that completes when state is saved</returns>
    Task SaveStateAsync<T>(string pageKey, T state) where T : class;

    /// <summary>
    /// Load page state for a specific page key
    /// </summary>
    /// <param name="pageKey">Unique key for the page (e.g., "inr-list")</param>
    /// <returns>The saved state or null if not found</returns>
    Task<T?> LoadStateAsync<T>(string pageKey) where T : class;

    /// <summary>
    /// Clear state for a specific page key
    /// </summary>
    /// <param name="pageKey">Unique key for the page</param>
    /// <returns>Task that completes when state is cleared</returns>
    Task ClearStateAsync(string pageKey);
}
