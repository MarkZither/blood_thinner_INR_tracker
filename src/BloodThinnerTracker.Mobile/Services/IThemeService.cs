using Microsoft.Maui;

namespace BloodThinnerTracker.Mobile.Services;

public interface IThemeService
{
    /// <summary>
    /// Get the current persisted theme preference (Unspecified = follow system).
    /// </summary>
    AppTheme GetCurrentTheme();

    /// <summary>
    /// Apply and persist the requested theme.
    /// </summary>
    void SetTheme(AppTheme theme);

    /// <summary>
    /// Cycle Light -> Dark -> Unspecified (follow system) -> Light
    /// </summary>
    AppTheme CycleTheme();
}
