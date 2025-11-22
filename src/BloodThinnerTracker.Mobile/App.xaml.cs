using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using BloodThinnerTracker.Mobile.Views;

namespace BloodThinnerTracker.Mobile
{
    public partial class App : Application
    {
        private readonly IServiceProvider _services;

        public App(IServiceProvider services)
        {
            _services = services;
            // Make service provider accessible to views for lazy service resolution
            ServiceHelper.Current = services;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Create AppShell with default login route
            var appShell = new AppShell();

            // Check authentication and navigate appropriately
            var authService = _services.GetRequiredService<Services.IAuthService>();
            var token = authService.GetAccessTokenAsync().GetAwaiter().GetResult();
            bool isAuthenticated = !string.IsNullOrEmpty(token);

            // Schedule navigation after shell is ready
            if (isAuthenticated)
            {
                // Navigate to home after shell displays
                appShell.Loaded += async (s, e) =>
                {
                    await appShell.GoToAsync("///flyouthome");
                };
            }
            else
            {
                // Navigate to login after shell displays
                appShell.Loaded += async (s, e) =>
                {
                    await appShell.GoToAsync("///login");
                };
            }

            return new Window(appShell);
        }
    }
}
