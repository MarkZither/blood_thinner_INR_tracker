using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BloodThinnerTracker.Mobile.Views;

namespace BloodThinnerTracker.Mobile
{
    public partial class App : Application
    {
        private readonly IServiceProvider _services;
        public static IServiceProvider? ServiceProvider { get; private set; }

        public App(IServiceProvider services)
        {
            _services = services;
            ServiceProvider = services;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Create AppShell via DI so constructor dependencies (IThemeService) are injected
            var appShell = _services.GetRequiredService<AppShell>();

            try
            {
                // Track cold-start metric (best-effort)
                var telemetry = _services.GetService<BloodThinnerTracker.Mobile.Services.Telemetry.ITelemetryService>();
                if (telemetry != null)
                {
                    var procStart = System.Diagnostics.Process.GetCurrentProcess().StartTime;
                    var coldMs = (DateTime.Now - procStart).TotalMilliseconds;
                    telemetry.TrackHistogram("ColdStartMs", coldMs);
                }
            }
            catch (Exception ex)
            {
                // Best-effort telemetry - log but don't fail startup
                var logger = _services.GetService<ILogger<App>>();
                logger?.LogDebug(ex, "Cold-start telemetry tracking failed");
            }

            // Schedule navigation after shell is ready. Perform initialization and auth check asynchronously
            var authService = _services.GetRequiredService<Services.IAuthService>();
            appShell.Loaded += async (s, e) =>
            {
                var logger = _services.GetService<Microsoft.Extensions.Logging.ILogger<App>>();
                var telemetry = _services.GetService<BloodThinnerTracker.Mobile.Services.Telemetry.ITelemetryService>();

                var splashOptions = _services.GetService<Microsoft.Extensions.Options.IOptions<Services.SplashOptions>>()?.Value;
                var showUntilInitialized = splashOptions?.ShowUntilInitialized ?? true;
                var timeoutMs = splashOptions?.TimeoutMs ?? 3000;

                if (showUntilInitialized)
                {
                    try
                    {
                        var initializer = _services.GetService<Services.IAppInitializer>();
                        if (initializer != null)
                        {
                            await initializer.InitializeAsync(TimeSpan.FromMilliseconds(timeoutMs));
                        }
                        else
                        {
                            // warm auth token with a timeout; do not block the UI thread
                            try
                            {
                                var authTask = authService.GetAccessTokenAsync();
                                var completed = await Task.WhenAny(authTask, Task.Delay(timeoutMs));
                                if (completed == authTask)
                                {
                                    _ = await authTask;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger?.LogWarning(ex, "Auth warm-up failed");
                                telemetry?.TrackEvent("AuthWarmupFailed");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Initialization failed in App.CreateWindow Loaded handler");
                        telemetry?.TrackEvent("AppInitializationFailed");
                    }
                }

                // Finally decide where to navigate based on async auth check (non-blocking)
                try
                {
                    var token = string.Empty;
                    try
                    {
                        token = await authService.GetAccessTokenAsync().ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogDebug(ex, "GetAccessTokenAsync failed during navigation decision");
                        telemetry?.TrackEvent("GetAccessTokenFailed");
                    }

                    bool isAuthenticated = !string.IsNullOrEmpty(token);
                    if (isAuthenticated)
                    {
                        await appShell.GoToAsync("///flyouthome");
                    }
                    else
                    {
                        await appShell.GoToAsync("///login");
                    }
                }
                catch (Exception ex)
                {
                    // Last-resort logging; avoid crashing the app.
                    var logger2 = _services.GetService<Microsoft.Extensions.Logging.ILogger<App>>();
                    logger2?.LogError(ex, "Navigation after initialization failed");
                    telemetry?.TrackEvent("NavigationFailed");
                }
            };

            return new Window(appShell);
        }
    }
}
