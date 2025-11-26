using System;
using Xunit;
using BloodThinnerTracker.Mobile.Services;

namespace Mobile.UnitTests
{
    public class KeyManagementServiceTests
    {
        [Fact(Skip = "KeyManagementService removed")]
        public void DeriveKey_Returns_CorrectLength()
        {
            // Test skipped because KeyManagementService was removed by request.
            Assert.True(true);
        }

        [Fact(Skip = "KeyManagementService removed")]
        public void GenerateSalt_Returns_DifferentValues()
        {
            // Test skipped because KeyManagementService was removed by request.
            Assert.True(true);
        }
    }
}
