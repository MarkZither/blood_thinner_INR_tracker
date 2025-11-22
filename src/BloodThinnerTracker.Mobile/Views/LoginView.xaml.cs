using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Views
{
    public partial class LoginView : ContentPage
    {
        private readonly ViewModels.LoginViewModel _vm;

        public LoginView(ViewModels.LoginViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            SignInButton.Clicked += SignInButton_Clicked;
        }

        private async void SignInButton_Clicked(object? sender, EventArgs e)
        {
            BusyIndicator.IsVisible = BusyIndicator.IsRunning = true;
            SignInButton.IsEnabled = false;
            try
            {
                var ok = await _vm.SignInAsync();
                if (ok)
                {
                    var window = Application.Current?.Windows is { Count: > 0 } ? Application.Current.Windows[0] : null;
                    var page = new NavigationPage(new MainPage());
                    if (window != null) window.Page = page; else Application.Current?.OpenWindow(new Window(page));
                }
                else
                {
                        await DisplayAlertAsync("Sign in failed", "Unable to sign in. Try again.", "OK");
                }
            }
            finally
            {
                BusyIndicator.IsVisible = BusyIndicator.IsRunning = false;
                SignInButton.IsEnabled = true;
            }
        }
    }
}
