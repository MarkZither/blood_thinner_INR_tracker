using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.ViewModels;

namespace BloodThinnerTracker.Mobile.Services
{
    public class MockInrService : IInrService
    {
        public async Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 5)
        {
            // Simulated latency
            await Task.Delay(300);

            var now = DateTime.UtcNow;
            var values = new decimal[] { 2.7m, 2.9m, 3.1m, 2.6m, 2.8m };

            return values.Take(count).Select((v, i) => new InrListItemVm
            {
                PublicId = Guid.NewGuid(),
                InrValue = v,
                TestDate = now.AddDays(-i),
                Notes = i == 0 ? "Most recent" : null,
                ReviewedByProvider = false
            });
        }
    }
}
