using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace BloodThinnerTracker.Mobile.Views
{
    /// <summary>
    /// Simple medicine list page. Triggers a foreground sync when the view appears.
    /// Implemented as a code-only page to avoid XAML static resource dependencies in unit tests.
    /// </summary>
    public class MedicineListView : ContentPage
    {
        private readonly Services.IForegroundSyncService _foregroundSync;
        private readonly ILogger<MedicineListView>? _logger;

        public MedicineListView(Services.IForegroundSyncService foregroundSync)
        {
            _foregroundSync = foregroundSync ?? throw new ArgumentNullException(nameof(foregroundSync));
            // Resolve a logger from the app-level service provider if available
            _logger = App.ServiceProvider?.GetService<ILogger<MedicineListView>>();

            Title = "Medications";
            Content = new StackLayout
            {
                Children = {
                    new Label { Text = "Medications" }
                }
            };
        }

        // Parameterless constructor for frameworks that may require it
        public MedicineListView()
            : this(App.ServiceProvider?.GetRequiredService<Services.IForegroundSyncService>() ?? throw new InvalidOperationException("ServiceProvider not initialized"))
        {
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Fire-and-forget background sync trigger; do not block UI
                _ = _foregroundSync.EnsureForegroundSyncAsync();
            }
            catch (Exception ex)
            {
                // Log the exception and optionally capture telemetry; do not rethrow to avoid disrupting UI
                _logger?.LogWarning(ex, "Foreground sync failed in {View}.OnAppearing", nameof(MedicineListView));
                try
                {
                    var telemetry = App.ServiceProvider?.GetService<Services.Telemetry.ITelemetryService>();
                    telemetry?.TrackEvent("ForegroundSync.Failed", new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "View", nameof(MedicineListView) },
                        { "Message", ex.Message }
                    });
                }
                catch (Exception telemetryEx)
                {
                    _logger?.LogDebug(telemetryEx, "Failed to report telemetry for ForegroundSync failure");
                }
            }
        }
    }
}
