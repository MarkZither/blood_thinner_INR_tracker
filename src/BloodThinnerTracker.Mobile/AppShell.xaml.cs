namespace BloodThinnerTracker.Mobile;

public partial class AppShell : Shell
{
    private readonly Services.IThemeService _themeService;

    public AppShell(Services.IThemeService themeService)
    {
        if (themeService == null) throw new ArgumentNullException(nameof(themeService));

        InitializeComponent();
        _themeService = themeService;

        // Apply persisted theme at shell creation (guard Application.Current)
        var theme = _themeService.GetCurrentTheme();
        if (Application.Current != null)
        {
            try { Application.Current.UserAppTheme = theme; }
            catch { }
        }
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
        catch { }
    }

    private async void OnFlyoutAboutClicked(object? sender, EventArgs e)
    {
        try
        {
            FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("///flyoutabout/about");
        }
        catch { }
    }

    private async void OnFlyoutLoginClicked(object? sender, EventArgs e)
    {
        try
        {
            FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("///login");
        }
        catch { }
    }
}
