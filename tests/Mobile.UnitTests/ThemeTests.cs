using System;
using Xunit;
using Microsoft.Maui.Controls;

namespace Mobile.UnitTests
{
    /// <summary>
    /// Theme tests require full MAUI runtime to load XAML resources.
    /// These tests are skipped in CI but can be run on device/emulator.
    /// </summary>
    [Trait("Category", "Integration")]
    public class ThemeTests : IDisposable
    {
        public ThemeTests()
        {
            // These tests require MAUI runtime to load XAML resources
            // They are skipped in CI but can be run on device/emulator
        }

        [Fact(Skip = "Requires MAUI runtime to load XAML resources")]
        public void AppColors_AreRegistered()
        {
            var resources = Application.Current?.Resources;
            Assert.NotNull(resources);
            Assert.True(resources.ContainsKey("PrimaryColor"));
            Assert.True(resources.ContainsKey("PrimaryBrush"));
            Assert.True(resources.ContainsKey("AccentColor"));
            Assert.True(resources.ContainsKey("ForegroundBrush"));
        }

        [Fact(Skip = "Requires MAUI runtime to load XAML resources")]
        public void AppStyles_AreRegistered()
        {
            var resources = Application.Current?.Resources;
            Assert.NotNull(resources);
            Assert.True(resources.MergedDictionaries.Count >= 2);
            // Check that a style key exists
            Assert.True(resources.ContainsKey("PrimaryButton") || resources.ContainsKey("DefaultLabel") );
        }

        public void Dispose()
        {
            // Tear down Application.Current to avoid side effects
            Application.Current = null;
        }
    }
}
