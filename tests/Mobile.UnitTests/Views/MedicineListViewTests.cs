using System.Reflection;
using System.Threading.Tasks;
using Moq;
using BloodThinnerTracker.Mobile.Services;
using Xunit;

namespace BloodThinnerTracker.Mobile.UnitTests.Views
{
    public class MedicineListViewTests
    {
        [Fact]
        public void OnAppearing_TriggersForegroundSync()
        {
            var fg = new Mock<IForegroundSyncService>();
            fg.Setup(f => f.EnsureForegroundSyncAsync()).Returns(Task.CompletedTask).Verifiable();

            // Make App.ServiceProvider available for the view's OnAppearing (set via non-public setter)
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddSingleton<IForegroundSyncService>(fg.Object);
            var sp = services.BuildServiceProvider();
            var prop = typeof(BloodThinnerTracker.Mobile.App).GetProperty("ServiceProvider", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var setter = prop?.GetSetMethod(true);
            setter?.Invoke(null, [sp]);

            var medSvc = new Mock<BloodThinnerTracker.Mobile.Services.IMedicationService>();
            medSvc.Setup(m => m.GetMedicationsAsync()).ReturnsAsync(new System.Collections.Generic.List<Shared.Models.Medication>());

            var vm = new BloodThinnerTracker.Mobile.ViewModels.MedicineListViewModel(medSvc.Object);
            var factory = new Mock<BloodThinnerTracker.Mobile.Extensions.LazyViewModelFactory<BloodThinnerTracker.Mobile.ViewModels.MedicineListViewModel>>();
            factory.Setup(f => f.CreateLazy()).Returns(new Lazy<BloodThinnerTracker.Mobile.ViewModels.MedicineListViewModel>(() => vm));

            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<BloodThinnerTracker.Mobile.Views.MedicineListView>>();
            var page = new BloodThinnerTracker.Mobile.Views.MedicineListView(factory.Object, logger.Object);

            // Call protected OnAppearing via reflection
            var mi = typeof(BloodThinnerTracker.Mobile.Views.MedicineListView).GetMethod("OnAppearing", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            Assert.NotNull(mi);
            mi.Invoke(page, new object[] { });

            fg.Verify(f => f.EnsureForegroundSyncAsync(), Times.Once);
        }

        [Fact]
        public async Task LoadMedicationsAsync_PopulatesList()
        {
            var fg = new Mock<IForegroundSyncService>();
            fg.Setup(f => f.EnsureForegroundSyncAsync()).Returns(Task.CompletedTask);

            var med1 = new Shared.Models.Medication { Id = 1, PublicId = Guid.NewGuid(), Name = "MedA", Dosage = 1m, DosageUnit = "mg" };
            var med2 = new Shared.Models.Medication { Id = 2, PublicId = Guid.NewGuid(), Name = "MedB", Dosage = 2m, DosageUnit = "mg" };

            var medSvc = new Mock<BloodThinnerTracker.Mobile.Services.IMedicationService>();
            medSvc.Setup(m => m.GetMedicationsAsync()).ReturnsAsync(new System.Collections.Generic.List<Shared.Models.Medication> { med1, med2 });

            var vm = new BloodThinnerTracker.Mobile.ViewModels.MedicineListViewModel(medSvc.Object);

            // Execute ViewModel's load command
            await vm.LoadMedicationsCommand.ExecuteAsync(null);

            Assert.Equal(2, vm.Medications.Count);
        }
    }
}
