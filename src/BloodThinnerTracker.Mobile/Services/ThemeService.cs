using Microsoft.Maui;
using Microsoft.Maui.Storage;

namespace BloodThinnerTracker.Mobile.Services;

public class ThemeService : IThemeService
{
    private const string PrefKey = "UserThemePreference";

    public AppTheme GetCurrentTheme()
    {
        var value = Preferences.Get(PrefKey, "Unspecified");
        return Parse(value);
    }

    public void SetTheme(AppTheme theme)
    {
        Preferences.Set(PrefKey, theme.ToString());
        ApplyTheme(theme);
    }

    public AppTheme CycleTheme()
    {
        var current = GetCurrentTheme();
        AppTheme next = current switch
        {
            AppTheme.Unspecified => AppTheme.Light,
            AppTheme.Light => AppTheme.Dark,
            AppTheme.Dark => AppTheme.Unspecified,
            _ => AppTheme.Unspecified
        };

        SetTheme(next);
        return next;
    }

    private static void ApplyTheme(AppTheme theme)
    {
        try
        {
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = theme;
            }
        }
        catch
        {
            // Best-effort; avoid throwing in UI service
        }
    }

    private static AppTheme Parse(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
    }
}
