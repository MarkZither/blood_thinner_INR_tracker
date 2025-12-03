using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using BloodThinnerTracker.Mobile.Views;
using BloodThinnerTracker.Mobile.ViewModels;
using BloodThinnerTracker.Mobile.Services;

namespace Mobile.UnitTests.Views
{
    /// <summary>
    /// View tests require MAUI runtime to load XAML and StaticResources.
    /// These tests are skipped in CI but can be run on device/emulator.
    /// </summary>
    [Trait("Category", "Integration")]
    public class InrListViewLazyFactoryTests
    {
        private class TestInrService : IInrService
        {
            public Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count) => Task.FromResult<IEnumerable<InrListItemVm>>(Array.Empty<InrListItemVm>());
        }

        private class TestCacheService : ICacheService
        {
            public Task SetAsync(string key, string jsonPayload, TimeSpan? expiresIn = null) => Task.CompletedTask;
            public Task<string?> GetAsync(string key) => Task.FromResult<string?>(null);
            public Task<bool> HasValidCacheAsync(string key) => Task.FromResult(false);
            public Task<long?> GetCacheAgeMillisecondsAsync(string key) => Task.FromResult<long?>(null);
            public Task ClearAsync(string key) => Task.CompletedTask;
            public Task<DateTime?> GetExpirationTimeAsync(string key) => Task.FromResult<DateTime?>(null);
        }

        private class TestInrListViewModel : InrListViewModel
        {
            public static int ConstructedCount = 0;

            public TestInrListViewModel(IInrService inr, IInrRepository? repo = null) : base(inr, repo)
            {
                ConstructedCount++;
            }
        }

        [Fact(Skip = "Requires MAUI runtime to load XAML StaticResources - run on device/emulator")]
        public void ViewModel_Is_Not_Created_Until_OnAppearing()
        {
            // Arrange
            TestInrListViewModel.ConstructedCount = 0;
            var services = new ServiceCollection();
            services.AddTransient<IInrService, TestInrService>();
            // IInrRepository is optional, so we don't need to register it
            // Register TestInrListViewModel as the concrete type for InrListViewModel
            services.AddTransient<InrListViewModel, TestInrListViewModel>();
            services.AddTransient(typeof(BloodThinnerTracker.Mobile.Extensions.LazyViewModelFactory<>));

            var sp = services.BuildServiceProvider();

            var factory = sp.GetRequiredService<BloodThinnerTracker.Mobile.Extensions.LazyViewModelFactory<InrListViewModel>>();
            var logger = NullLogger<InrListView>.Instance;

            if (Microsoft.Maui.Controls.Application.Current == null)
            {
                Microsoft.Maui.Controls.Application.Current = new Microsoft.Maui.Controls.Application();
            }
            var resources = Microsoft.Maui.Controls.Application.Current.Resources;
            resources["PageBackground"] = Microsoft.Maui.Graphics.Colors.White;
            resources["PrimaryBlue"] = Microsoft.Maui.Graphics.Color.FromArgb("#0066CC");
            resources["BackgroundWhite"] = Microsoft.Maui.Graphics.Colors.White;
            resources["TextLight"] = Microsoft.Maui.Graphics.Color.FromArgb("#999999");
            resources["TextMedium"] = Microsoft.Maui.Graphics.Color.FromArgb("#666666");
            resources["TextDark"] = Microsoft.Maui.Graphics.Color.FromArgb("#333333");
            resources["BorderGray"] = Microsoft.Maui.Graphics.Color.FromArgb("#CCCCCC");
            resources["BackgroundGray"] = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5");
            resources["SecondaryButton"] = new Microsoft.Maui.Controls.Style(typeof(Microsoft.Maui.Controls.Button));
            resources["CardFrame"] = new Microsoft.Maui.Controls.Style(typeof(Microsoft.Maui.Controls.Border));
            resources["AlertFrame"] = new Microsoft.Maui.Controls.Style(typeof(Microsoft.Maui.Controls.Border));
            resources["InvertedBoolConverter"] = new BloodThinnerTracker.Mobile.Converters.InvertedBoolConverter();
            resources["IsNotNullOrEmptyConverter"] = new BloodThinnerTracker.Mobile.Converters.IsNotNullOrEmptyConverter();

            var view = new InrListView(factory, logger);

            // Assert VM not constructed yet
            Assert.Equal(0, TestInrListViewModel.ConstructedCount);

            // Act - call protected OnAppearing via reflection
            var onAppearing = view.GetType().GetMethod("OnAppearing", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, System.Type.EmptyTypes, null);
            Assert.NotNull(onAppearing);
            onAppearing.Invoke(view, null);

            // Assert - VM constructed once and BindingContext set
            Assert.Equal(1, TestInrListViewModel.ConstructedCount);
            Assert.NotNull(view.BindingContext);
            Assert.IsType<TestInrListViewModel>(view.BindingContext);
        }
    }
}
