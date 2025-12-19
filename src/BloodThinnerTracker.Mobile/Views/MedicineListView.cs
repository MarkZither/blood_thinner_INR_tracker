using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
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

        public MedicineListView(Services.IForegroundSyncService foregroundSync)
        {
            _foregroundSync = foregroundSync ?? throw new ArgumentNullException(nameof(foregroundSync));

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
            catch
            {
                // Best-effort; swallow exceptions to avoid crashing UI
            }
        }
    }
}
