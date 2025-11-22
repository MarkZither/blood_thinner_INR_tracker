using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace BloodThinnerTracker.Mobile
{
    public partial class App : Application
    {
        private readonly IServiceProvider _services;

        public App(IServiceProvider services)
        {
            _services = services;
            InitializeComponent();
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Resolve SplashView from DI
            var splash = _services.GetService(typeof(Views.SplashView)) as Page
                         ?? ActivatorUtilities.CreateInstance(_services, typeof(Views.SplashView)) as Page;

            if (splash == null)
                throw new InvalidOperationException("Unable to create SplashView from DI");

            // Wrap in NavigationPage
            var nav = new NavigationPage(splash);

            // Return the initial window
            return new Window(nav);
        }

    }
}
