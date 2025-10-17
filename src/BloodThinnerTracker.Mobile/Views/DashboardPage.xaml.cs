using BloodThinnerTracker.Mobile.ViewModels;

namespace BloodThinnerTracker.Mobile.Views;

/// <summary>
/// Dashboard page showing key metrics and recent activity
/// </summary>
public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}