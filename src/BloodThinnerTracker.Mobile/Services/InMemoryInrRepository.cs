using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.ViewModels;

namespace BloodThinnerTracker.Mobile.Services
{
    // Lightweight in-memory IInrRepository used for tests and when platform DB isn't available.
    public class InMemoryInrRepository : IInrRepository
    {
        private readonly List<InrListItemVm> _store = new();

        public Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 10)
        {
            var items = _store.OrderByDescending(i => i.TestDate).Take(count).ToList();
            return Task.FromResult<IEnumerable<InrListItemVm>>(items);
        }

        public Task SaveRangeAsync(IEnumerable<InrListItemVm> items)
        {
            if (items == null) return Task.CompletedTask;

            foreach (var it in items)
            {
                var existing = _store.FirstOrDefault(x => x.PublicId == it.PublicId);
                if (existing == null) _store.Add(it);
                else
                {
                    existing.TestDate = it.TestDate;
                    existing.InrValue = it.InrValue;
                    existing.Notes = it.Notes;
                    existing.ReviewedByProvider = it.ReviewedByProvider;
                }
            }

            return Task.CompletedTask;
        }
    }
}
