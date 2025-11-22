using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Views
{
    public partial class SplashView : ContentPage
    {
        private readonly ViewModels.SplashViewModel _vm;
        private bool _cancelAnimation;

        public SplashView(ViewModels.SplashViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _cancelAnimation = false;
            _ = RunPulseLoopAsync();

            await Task.Delay(150); // let UI settle
            await _vm.InitializeAsync();

            // navigate based on authentication state
            var window = Application.Current?.Windows is { Count: > 0 } ? Application.Current.Windows[0] : null;
            if (_vm.IsAuthenticated)
            {
                var page = new NavigationPage(new MainPage());
                if (window != null) window.Page = page; else Application.Current?.OpenWindow(new Window(page));
            }
            else
            {
                var page = new NavigationPage(new LoginView(_vm.AuthViewModel));
                if (window != null) window.Page = page; else Application.Current?.OpenWindow(new Window(page));
            }
        }

        protected override void OnDisappearing()
        {
            _cancelAnimation = true;
            base.OnDisappearing();
        }

        private async Task RunPulseLoopAsync()
        {
            while (!_cancelAnimation)
            {
                try
                {
                    var img = LogoImage;
                    if (img == null) break;
                    await img.ScaleToAsync(1.08, 600u);
                    await img.ScaleToAsync(0.96, 600u);
                }
                catch
                {
                    // animation canceled or view disposed
                    break;
                }
            }
        }
    }
}
