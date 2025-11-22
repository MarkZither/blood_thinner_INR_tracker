using Microsoft.Maui.Controls;

namespace BloodThinnerTracker.Mobile.Views
{
    /// <summary>
    /// Login screen for OAuth authentication.
    /// Uses RelayCommand pattern from MVVM Toolkit for sign-in flow.
    /// </summary>
    public partial class LoginView : ContentPage
    {
        public LoginView(ViewModels.LoginViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}

