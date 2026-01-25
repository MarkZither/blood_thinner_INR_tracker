using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BloodThinnerTracker.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace BloodThinnerTracker.Mobile.ViewModels
{
    public partial class MedicineListViewModel : ObservableObject
    {
        private readonly IMedicationService _medService;
        private readonly Services.Telemetry.ITelemetryService? _telemetry;
        private readonly ILogger<MedicineListViewModel>? _logger;

        [ObservableProperty]
        private ObservableCollection<Shared.Models.Medication> medications = new();

        [ObservableProperty]
        private bool isBusy = false;

        [ObservableProperty]
        private bool showList = false;

        [ObservableProperty]
        private bool showEmpty = false;

        [ObservableProperty]
        private string lastUpdatedText = "Never updated";

        public MedicineListViewModel(IMedicationService medService, Services.Telemetry.ITelemetryService? telemetry = null, ILogger<MedicineListViewModel>? logger = null)
        {
            _medService = medService ?? throw new ArgumentNullException(nameof(medService));
            _telemetry = telemetry;
            _logger = logger;
        }

        [RelayCommand]
        public async Task LoadMedications()
        {
            if (IsBusy) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                IsBusy = true;
                ShowEmpty = false;
                ShowList = false;

                var meds = await _medService.GetMedicationsAsync();
                Medications = new ObservableCollection<Shared.Models.Medication>(meds ?? new System.Collections.Generic.List<Shared.Models.Medication>());

                if (Medications.Count > 0)
                {
                    ShowList = true;
                    LastUpdatedText = $"Updated {DateTimeOffset.UtcNow:u}";
                }
                else
                {
                    ShowEmpty = true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load medications");
                ShowEmpty = true;
            }
            finally
            {
                IsBusy = false;
                try
                {
                    sw.Stop();
                    _telemetry?.TrackHistogram("MedicineList.LoadMs", sw.Elapsed.TotalMilliseconds);
                }
                catch { }
            }
        }
    }
}
