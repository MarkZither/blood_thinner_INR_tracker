using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Text.Json;

namespace BloodThinnerTracker.Web.Services;

/// <summary>
/// Implementation of page state service using ProtectedSessionStorage.
/// Works in both server-side and client-side Blazor rendering modes.
/// </summary>
public class PageStateService : IPageStateService
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly ILogger<PageStateService> _logger;
    private const string StateKeyPrefix = "PageState_";

    public PageStateService(
        ProtectedSessionStorage sessionStorage,
        ILogger<PageStateService> logger)
    {
        _sessionStorage = sessionStorage;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SaveStateAsync<T>(string pageKey, T state) where T : class
    {
        if (string.IsNullOrWhiteSpace(pageKey))
        {
            throw new ArgumentException("Page key cannot be null or empty", nameof(pageKey));
        }

        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        try
        {
            var key = GetStorageKey(pageKey);
            await _sessionStorage.SetAsync(key, state);
            _logger.LogDebug("Saved state for page key: {PageKey}", pageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving state for page key: {PageKey}", pageKey);
            // Don't throw - state persistence is non-critical functionality
        }
    }

    /// <inheritdoc/>
    public async Task<T?> LoadStateAsync<T>(string pageKey) where T : class
    {
        if (string.IsNullOrWhiteSpace(pageKey))
        {
            throw new ArgumentException("Page key cannot be null or empty", nameof(pageKey));
        }

        try
        {
            var key = GetStorageKey(pageKey);
            var result = await _sessionStorage.GetAsync<T>(key);
            
            if (result.Success)
            {
                _logger.LogDebug("Loaded state for page key: {PageKey}", pageKey);
                return result.Value;
            }
            
            _logger.LogDebug("No saved state found for page key: {PageKey}", pageKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading state for page key: {PageKey}", pageKey);
            return null; // Return null on error - let the page use defaults
        }
    }

    /// <inheritdoc/>
    public async Task ClearStateAsync(string pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey))
        {
            throw new ArgumentException("Page key cannot be null or empty", nameof(pageKey));
        }

        try
        {
            var key = GetStorageKey(pageKey);
            await _sessionStorage.DeleteAsync(key);
            _logger.LogDebug("Cleared state for page key: {PageKey}", pageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing state for page key: {PageKey}", pageKey);
            // Don't throw - state clearing is non-critical functionality
        }
    }

    private static string GetStorageKey(string pageKey)
    {
        return $"{StateKeyPrefix}{pageKey}";
    }
}
