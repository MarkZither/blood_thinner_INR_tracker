using BloodThinnerTracker.Mobile.Services;
using System.Windows.Input;

namespace BloodThinnerTracker.Mobile.ViewModels;

/// <summary>
/// ViewModel for the AppShell navigation and flyout
/// </summary>
public class AppShellViewModel : BaseViewModel
{
    private readonly ISyncService _syncService;
    private readonly INotificationService _notificationService;

    public AppShellViewModel()
    {
        // Note: In a real app, these would be injected
        // For now, we'll create simplified implementations
        
        SyncCommand = new AsyncRelayCommand(SyncDataAsync);
        EmergencyContactCommand = new AsyncRelayCommand(ShowEmergencyContactAsync);
    }

    public ICommand SyncCommand { get; }
    public ICommand EmergencyContactCommand { get; }

    private async Task SyncDataAsync()
    {
        // Implementation would sync data with the server
        await Task.Delay(1000);
    }

    private async Task ShowEmergencyContactAsync()
    {
        await Shell.Current.GoToAsync("emergency");
    }
}

/// <summary>
/// ViewModel for the main page (used when not authenticated)
/// </summary>
public class MainViewModel : BaseViewModel
{
    public MainViewModel()
    {
        Title = "Blood Thinner Tracker";
    }
}

/// <summary>
/// Placeholder ViewModels for the other pages
/// </summary>
public class MedicationViewModel : BaseViewModel
{
    public MedicationViewModel()
    {
        Title = "Medications";
    }
}

public class INRTrackingViewModel : BaseViewModel
{
    public INRTrackingViewModel()
    {
        Title = "INR Tracking";
    }
}

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;

    public LoginViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        Title = "Login";
        
        LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
        RegisterCommand = new AsyncRelayCommand(NavigateToRegisterAsync);
    }

    public string Email
    {
        get => _email;
        set
        {
            SetProperty(ref _email, value);
            ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
        }
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Email) && 
               !string.IsNullOrWhiteSpace(Password) && 
               !IsBusy;
    }

    private async Task LoginAsync()
    {
        await ExecuteAsync(async () =>
        {
            var success = await _authService.LoginAsync(Email, Password);
            if (success)
            {
                // Navigate to main app
                if (Application.Current is App app)
                {
                    app.NavigateToMainApp();
                }
            }
            else
            {
                SetError("Invalid email or password. Please try again.");
            }
        });
    }

    private async Task NavigateToRegisterAsync()
    {
        await Shell.Current.GoToAsync("register");
    }
}

public class ProfileViewModel : BaseViewModel
{
    public ProfileViewModel()
    {
        Title = "Profile";
    }
}