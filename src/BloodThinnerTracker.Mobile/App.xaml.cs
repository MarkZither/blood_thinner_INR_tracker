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

            // Resolve MainPage from DI when available
            MainPage = _services.GetService(typeof(MainPage)) as Page ?? new MainPage();
        }
    }
}
