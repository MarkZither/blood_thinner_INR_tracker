using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile;

public partial class AppShell : Shell
{
    private readonly Services.IThemeService _themeService;
    private readonly ILogger<AppShell>? _logger;

    public AppShell(Services.IThemeService themeService, ILogger<AppShell>? logger = null)
    {
        if (themeService == null) throw new ArgumentNullException(nameof(themeService));
        _logger = logger;
        _logger?.LogInformation("AppShell constructor called");

        // Assign injected services before calling InitializeComponent so any
        // XAML-instantiated elements or event handlers that access these
        // fields during load won't encounter null references.
        _themeService = themeService;
        InitializeComponent();

        // Apply persisted theme at shell creation
        try
        {
            var theme = _themeService.GetCurrentTheme();
            _themeService.SetTheme(theme); // Use SetTheme instead of ApplyTheme
            _logger?.LogInformation("Theme applied successfully: {Theme}", theme);
        }
        catch (System.Exception ex)
        {
            _logger?.LogError(ex, "Failed to apply theme in AppShell constructor");
        }

        _logger?.LogInformation("AppShell initialized successfully");
    }

    private void OnThemeToggleClicked(object? sender, EventArgs e)
    {
        var next = _themeService.CycleTheme();
        // No UI feedback here; theme change is applied immediately.
    }

    private async void OnFlyoutHomeClicked(object? sender, EventArgs e)
    {
        try
        {
            FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("///flyouthome/inrlist");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppShell: Navigation to flyouthome failed: {ex.Message}");
        }
    }

    private async void OnFlyoutAboutClicked(object? sender, EventArgs e)
    {
        try
        {
            FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("///flyoutabout/about");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppShell: Navigation to flyoutabout failed: {ex.Message}");
        }
    }

    private async void OnFlyoutLoginClicked(object? sender, EventArgs e)
    {
        try
        {
            FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("///login");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppShell: Navigation to login failed: {ex.Message}");
        }
    }

    private async void OnFlyoutDebugClicked(object? sender, EventArgs e)
    {
        try
        {
            FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("///debugscheduledreminders");
        }
            catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppShell: Navigation to debug page failed: {ex.Message}");
            _logger?.LogWarning(ex, "Navigation to debug page route failed; attempting fallback push");
            try
            {
                // Fallback: attempt to push using the active Window/Page navigation (avoid deprecated Application.MainPage)
                var nav = Shell.Current?.Navigation
                          ?? Application.Current?.Windows?.FirstOrDefault()?.Page?.Navigation;
                if (nav != null)
                {
                    await nav.PushAsync(new Views.DebugScheduledRemindersPage());
                }
                else
                {
                    _logger?.LogWarning("No navigation context available to push DebugScheduledRemindersPage");
                }
            }
            catch (Exception innerEx)
            {
                _logger?.LogDebug(innerEx, "Fallback PushAsync to DebugScheduledRemindersPage failed");
            }
        }
    }

    private async void OnFlyoutMedicationClicked(object? sender, EventArgs e)
    {
        try
        {
            FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("///flyoutmeds/medications");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppShell: Navigation to medications failed: {ex.Message}");
            _logger?.LogWarning(ex, "Navigation to medications route failed; attempting fallback push");
            try
            {
                var nav = Shell.Current?.Navigation ?? Application.Current?.Windows?.FirstOrDefault()?.Page?.Navigation;
                if (nav != null)
                {
                    await nav.PushAsync(new Views.MedicineListView());
                }
                else
                {
                    _logger?.LogWarning("No navigation context available to push MedicineListView");
                }
            }
            catch (Exception innerEx)
            {
                _logger?.LogDebug(innerEx, "Fallback PushAsync to MedicineListView failed");
            }
        }
    }
}
