using Xunit;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using BloodThinnerTracker.Mobile.Services;

namespace Mobile.UnitTests
{
    /// <summary>
    /// Theme switcher tests require MAUI platform services (Preferences).
    /// These tests are skipped in CI but can be run on device/emulator.
    /// </summary>
    [Trait("Category", "Integration")]
    public class ThemeSwitcherTests
    {
        public ThemeSwitcherTests()
        {
            if (Application.Current == null)
            {
                Application.Current = new Application();
                Application.Current.Resources = new ResourceDictionary();
            }
        }

        [Fact(Skip = "Requires MAUI platform services (Preferences) - run on device/emulator")]
        public void ThemeService_PersistsAndAppliesTheme()
        {
            var svc = new ThemeService();

            svc.SetTheme(AppTheme.Light);
            Assert.Equal(AppTheme.Light, svc.GetCurrentTheme());
            Assert.NotNull(Application.Current);
            Assert.Equal(AppTheme.Light, Application.Current.UserAppTheme);

            svc.SetTheme(AppTheme.Dark);
            Assert.Equal(AppTheme.Dark, svc.GetCurrentTheme());
            Assert.Equal(AppTheme.Dark, Application.Current.UserAppTheme);

            var cycled = svc.CycleTheme();
            // After Dark -> Unspecified
            Assert.Equal(AppTheme.Unspecified, cycled);
            Assert.Equal(AppTheme.Unspecified, svc.GetCurrentTheme());
        }
    }
}
