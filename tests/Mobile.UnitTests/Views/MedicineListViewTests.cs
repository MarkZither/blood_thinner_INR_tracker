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
            var fg = new Mock<BloodThinnerTracker.Mobile.Services.IForegroundSyncService>();
            fg.Setup(f => f.EnsureForegroundSyncAsync()).Returns(Task.CompletedTask).Verifiable();

            var page = new BloodThinnerTracker.Mobile.Views.MedicineListView(fg.Object);

            // Call protected OnAppearing via reflection
            var mi = typeof(BloodThinnerTracker.Mobile.Views.MedicineListView).GetMethod("OnAppearing", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            Assert.NotNull(mi);
            mi.Invoke(page, new object[] { });

            fg.Verify(f => f.EnsureForegroundSyncAsync(), Times.Once);
        }
    }
}
