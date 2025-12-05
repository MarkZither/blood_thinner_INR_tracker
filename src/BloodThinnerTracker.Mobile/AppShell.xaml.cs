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

        InitializeComponent();
        _themeService = themeService;

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
}
