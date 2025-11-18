using System;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class HealthController_Tests
    {
        [Fact]
        public void Health_ReturnsOkObjectResult()
        {
            // Arrange
            var controller = new HealthController();

            // Act
            var result = controller.Health();

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void Health_ReturnsStatusHealthyAndRecentTimestamp()
        {
            // Arrange
            var controller = new HealthController();

            // Act
            var result = controller.Health() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var value = result.Value;
            Assert.NotNull(value);

            // The controller returns an anonymous object with 'status' and 'timestamp'.
            // Use reflection to access properties in a case-insensitive way so the
            // assertion is robust to how the object is constructed/serialized.
            var valueType = value.GetType();
            var statusProp = valueType.GetProperty("status") ?? valueType.GetProperty("Status");
            Assert.NotNull(statusProp);
            var status = statusProp.GetValue(value) as string;

            var timestampProp = valueType.GetProperty("timestamp") ?? valueType.GetProperty("Timestamp");
            Assert.NotNull(timestampProp);
            var timestampObj = timestampProp.GetValue(value);
            Assert.IsType<DateTime>(timestampObj);
            var timestamp = (DateTime)timestampObj;

            Assert.Equal("Healthy", status);
            var age = DateTime.UtcNow - timestamp;
            Assert.True(age.TotalSeconds < 10, "Timestamp should be recent");
        }
    }
}
