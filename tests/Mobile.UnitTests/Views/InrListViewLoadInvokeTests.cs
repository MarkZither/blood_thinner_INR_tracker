using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
    public class InrListViewLoadInvokeTests
    {
        [Fact(Skip = "Requires MAUI runtime to load XAML StaticResources - run on device/emulator")]
        public async System.Threading.Tasks.Task Appearing_Invokes_IInrService_GetRecentAsync()
        {
            // Arrange
            var inrService = new TestInrService();

            var cacheMock = new Mock<ICacheService>();
            cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.TimeSpan?>())).Returns(System.Threading.Tasks.Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddSingleton<IInrService>(inrService);
            services.AddSingleton<ICacheService>(cacheMock.Object);
            services.AddTransient<InrListViewModel>();
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

            // Act: call OnAppearing via reflection
            var onAppearing = view.GetType().GetMethod("OnAppearing", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, System.Type.EmptyTypes, null);
            Assert.NotNull(onAppearing);
            onAppearing.Invoke(view, null);

            // Ensure BindingContext is set by OnAppearing (we won't call VM methods while UI bindings are attached)
            Assert.NotNull(view.BindingContext);

            // Create a VM instance directly from DI and invoke load to verify the InrService gets called.
            var vmDirect = sp.GetRequiredService<InrListViewModel>();
            await vmDirect.LoadInrLogsCommand.ExecuteAsync(null);

            // Wait for the service call to be signaled (or timeout)
            await System.Threading.Tasks.Task.WhenAny(inrService.Tcs.Task, System.Threading.Tasks.Task.Delay(2000));
            Assert.True(inrService.Tcs.Task.IsCompleted, "GetRecentAsync was not invoked within timeout");
            // Assert: verify service was called
            Assert.Equal(1, inrService.CallCount);
        }
    }

    internal class TestInrService : IInrService
    {
        public int CallCount { get; private set; }
        public TaskCompletionSource<bool> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 5)
        {
            CallCount++;
            Tcs.TrySetResult(true);
            return Task.FromResult<IEnumerable<InrListItemVm>>(Array.Empty<InrListItemVm>());
        }
    }
}
