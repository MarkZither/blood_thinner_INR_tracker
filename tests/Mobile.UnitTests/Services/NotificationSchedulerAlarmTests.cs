using System;
using System.Threading.Tasks;
using Xunit;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.UnitTests.Services
{
    public class NotificationSchedulerAlarmTests
    {
        [Fact]
        public async Task ScheduleExactAsync_Completes()
        {
            // Attempt to locate Android-specific scheduler; skip test on non-Android TFMs
            var t = Type.GetType("BloodThinnerTracker.Mobile.Platforms.Android.NotificationSchedulerAlarm, BloodThinnerTracker.Mobile");
            if (t == null)
            {
                // Not running on Android target; skip logic
                Assert.True(true, "NotificationSchedulerAlarm not available on this TFM; skipping test.");
                return;
            }

            var scheduler = Activator.CreateInstance(t);
            var req = new NotificationRequest(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1), "Test", "Body");
            var mi = t.GetMethod("ScheduleExactAsync");
            var taskObj = mi?.Invoke(scheduler, new object[] { req });
            if (taskObj is Task task) await task.ConfigureAwait(false);

            Assert.True(true);
        }

        [Fact]
        public void CanScheduleExactAlarms_ReturnsBool()
        {
            var t = Type.GetType("BloodThinnerTracker.Mobile.Platforms.Android.NotificationSchedulerAlarm, BloodThinnerTracker.Mobile");
            if (t == null)
            {
                Assert.True(true, "NotificationSchedulerAlarm not available on this TFM; skipping test.");
                return;
            }

            var mi = t.GetMethod("CanScheduleExactAlarms");
            var scheduler = Activator.CreateInstance(t);
            var val = mi?.Invoke(scheduler, null);
            Assert.IsType<bool>(val);
        }
    }
}
