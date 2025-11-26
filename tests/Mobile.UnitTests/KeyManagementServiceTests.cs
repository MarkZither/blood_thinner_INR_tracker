using System;
using Xunit;
using BloodThinnerTracker.Mobile.Services;

namespace Mobile.UnitTests
{
    public class KeyManagementServiceTests
    {
        [Fact]
        public void DeriveKey_Returns_CorrectLength()
        {
            var svc = new KeyManagementService();
            var salt = svc.GenerateSalt(16);
            var key = svc.DeriveKey("password123", salt, 1000);
            Assert.NotNull(key);
            Assert.Equal(32, key.Length);
        }

        [Fact]
        public void GenerateSalt_Returns_DifferentValues()
        {
            var svc = new KeyManagementService();
            var s1 = svc.GenerateSalt(16);
            var s2 = svc.GenerateSalt(16);
            Assert.NotEqual(Convert.ToBase64String(s1), Convert.ToBase64String(s2));
        }
    }
}
