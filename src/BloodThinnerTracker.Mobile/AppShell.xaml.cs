using BloodThinnerTracker.Mobile.ViewModels;

namespace BloodThinnerTracker.Mobile;

/// <summary>
/// Application Shell for navigation and layout
/// </summary>
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for pages that aren't in the shell structure
        Routing.RegisterRoute("login", typeof(Views.LoginPage));
        Routing.RegisterRoute("register", typeof(Views.RegisterPage));
        Routing.RegisterRoute("medication/add", typeof(Views.AddMedicationPage));
        Routing.RegisterRoute("medication/edit", typeof(Views.EditMedicationPage));
        Routing.RegisterRoute("inr/add", typeof(Views.AddINRTestPage));
        Routing.RegisterRoute("inr/history", typeof(Views.INRHistoryPage));
        Routing.RegisterRoute("notifications", typeof(Views.NotificationsPage));
        Routing.RegisterRoute("emergency", typeof(Views.EmergencyPage));

        BindingContext = new AppShellViewModel();
    }
}