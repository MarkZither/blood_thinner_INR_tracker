using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Mobile.Views;

namespace BloodThinnerTracker.Mobile;

/// <summary>
/// Main Application class for Blood Thinner Tracker Mobile App
/// </summary>
public partial class App : Application
{
    private readonly IAuthenticationService _authService;
    private readonly INotificationService _notificationService;

    public App(IAuthenticationService authService, INotificationService notificationService)
    {
        InitializeComponent();
        
        _authService = authService;
        _notificationService = notificationService;

        // Initialize the app
        InitializeApp();
    }

    private async void InitializeApp()
    {
        try
        {
            // Initialize notification service
            await _notificationService.InitializeAsync();

            // Check authentication status
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            
            if (isAuthenticated)
            {
                MainPage = new AppShell();
            }
            else
            {
                MainPage = new LoginPage();
            }
        }
        catch (Exception ex)
        {
            // Log error and show fallback UI
            System.Diagnostics.Debug.WriteLine($"App initialization error: {ex.Message}");
            MainPage = new LoginPage();
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        
        // App started - can be used for analytics
        System.Diagnostics.Debug.WriteLine("Blood Thinner Tracker app started");
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        
        // App going to sleep - save any pending data
        Task.Run(async () =>
        {
            try
            {
                // Auto-save any pending medication logs or INR data
                var syncService = Handler?.MauiContext?.Services?.GetService<ISyncService>();
                if (syncService != null)
                {
                    await syncService.SyncPendingDataAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during app sleep sync: {ex.Message}");
            }
        });
    }

    protected override void OnResume()
    {
        base.OnResume();
        
        // App resumed - refresh data if needed
        System.Diagnostics.Debug.WriteLine("Blood Thinner Tracker app resumed");
        
        Task.Run(async () =>
        {
            try
            {
                // Check for any missed notifications or reminders
                await _notificationService.CheckMissedRemindersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking missed reminders: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Navigate to authenticated shell after successful login
    /// </summary>
    public void NavigateToMainApp()
    {
        MainPage = new AppShell();
    }

    /// <summary>
    /// Navigate to login page after logout
    /// </summary>
    public void NavigateToLogin()
    {
        MainPage = new LoginPage();
    }
}