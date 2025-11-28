using System;
using Xunit;
using Microsoft.Maui.Controls;

namespace Mobile.UnitTests
{
    public class ThemeTests : IDisposable
    {
        public ThemeTests()
        {
            // Ensure Application is initialized for resource lookup
            if (Application.Current == null)
            {
                Application.Current = new Application();
                Application.Current.Resources = new ResourceDictionary();

                // Merge the theme dictionaries used by the app
                var colors = new ResourceDictionary();
                colors.Source = new Uri("Themes/AppColors.xaml", UriKind.RelativeOrAbsolute);
                Application.Current.Resources.MergedDictionaries.Add(colors);

                var styles = new ResourceDictionary();
                styles.Source = new Uri("Themes/AppStyles.xaml", UriKind.RelativeOrAbsolute);
                Application.Current.Resources.MergedDictionaries.Add(styles);
            }
        }

        [Fact]
        public void AppColors_AreRegistered()
        {
            var resources = Application.Current.Resources;
            Assert.True(resources.ContainsKey("PrimaryColor"));
            Assert.True(resources.ContainsKey("PrimaryBrush"));
            Assert.True(resources.ContainsKey("AccentColor"));
            Assert.True(resources.ContainsKey("ForegroundBrush"));
        }

        [Fact]
        public void AppStyles_AreRegistered()
        {
            var resources = Application.Current.Resources;
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
