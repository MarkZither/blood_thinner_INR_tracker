using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.ViewModels;
using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Mobile.Services.Telemetry;
using Xunit;

namespace Mobile.UnitTests
{
    public class PerfTests
    {
        private class FakeTelemetry : ITelemetryService
        {
            public readonly Dictionary<string, double> Metrics = new();
            public void TrackEvent(string name, IDictionary<string, string>? properties = null) { }
            public void TrackMetric(string name, double value)
            {
                Metrics[name] = value;
            }
            public void TrackHistogram(string name, double value)
            {
                // Map histogram to metrics dictionary for simple assertions
                Metrics[name] = value;
            }
        }

        private class FastInrService : IInrService
        {
            public Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count)
            {
                var list = Enumerable.Range(1, Math.Max(1, count)).Select(i => new InrListItemVm
                {
                    PublicId = Guid.NewGuid(),
                    TestDate = DateTime.UtcNow.AddDays(-i),
                    InrValue = 2.5m,
                    Notes = null,
                    ReviewedByProvider = false
                });
                return Task.FromResult(list);
            }
        }

        private class SimpleCache : ICacheService
        {
            private readonly Dictionary<string, (string value, DateTime ts)> _store = new();
            public Task<string?> GetAsync(string key)
            {
                _store.TryGetValue(key, out var v);
                return Task.FromResult<string?>(v.value);
            }
            public Task SetAsync(string key, string value, TimeSpan? ttl = null)
            {
                _store[key] = (value, DateTime.UtcNow);
                return Task.CompletedTask;
            }
            public Task<long?> GetCacheAgeMillisecondsAsync(string key)
            {
                if (_store.TryGetValue(key, out var v))
                    return Task.FromResult<long?>((long)(DateTime.UtcNow - v.ts).TotalMilliseconds);
                return Task.FromResult<long?>(null);
            }
            public Task<bool> HasValidCacheAsync(string key)
            {
                return Task.FromResult(_store.ContainsKey(key));
            }
            public Task ClearAsync(string key)
            {
                _store.Remove(key);
                return Task.CompletedTask;
            }
            public Task<DateTime?> GetExpirationTimeAsync(string key)
            {
                if (_store.TryGetValue(key, out var v))
                    return Task.FromResult<DateTime?>(v.ts.AddDays(7));
                return Task.FromResult<DateTime?>(null);
            }
        }

        [Fact]
        public async Task InrListLoad_RecordsTelemetry_And_IsFast()
        {
            // Arrange
            var telemetry = new FakeTelemetry();
            var inrService = new FastInrService();
            IInrRepository? repo = null; // Not needed for this performance test
            var vm = new InrListViewModel(inrService, repo, telemetry);

            // Act
            await vm.LoadInrLogs();

            // Assert
            Assert.True(telemetry.Metrics.ContainsKey("InrListLoadMs"), "Telemetry metric not recorded");
            var value = telemetry.Metrics["InrListLoadMs"];
            // Threshold SC-002: ensure load < 2000ms in CI with mocks
            Assert.True(value < 2000, $"InrListLoadMs too slow: {value}ms");
        }
    }
}
