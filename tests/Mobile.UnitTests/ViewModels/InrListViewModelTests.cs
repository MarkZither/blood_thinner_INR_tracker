using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Mobile.ViewModels;
using BloodThinnerTracker.Shared.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Mobile.UnitTests.ViewModels
{
    public class InrListViewModelTests
    {
        private class FakeInrRepository : IInrRepository
        {
            public bool SaveRangeCalled { get; private set; }
            public IEnumerable<InrListItemVm> StoredItems { get; private set; } = new List<InrListItemVm>();

            public Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 10)
            {
                // Return whatever was persisted, or empty if none
                return Task.FromResult((IEnumerable<InrListItemVm>)(StoredItems ?? new List<InrListItemVm>()));
            }

            public Task SaveRangeAsync(IEnumerable<InrListItemVm> items)
            {
                SaveRangeCalled = true;
                StoredItems = items.ToList();
                return Task.CompletedTask;
            }
        }

        private class FakeInrService : IInrService
        {
            private readonly IEnumerable<InrListItemVm> _items;
            public FakeInrService(IEnumerable<InrListItemVm> items)
            {
                _items = items;
            }

            public Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 5)
            {
                return Task.FromResult(_items.Take(count));
            }
        }

        [Fact]
        public async Task LoadInrLogs_PersistsApiResults_WhenRepositoryAvailable()
        {
            // Arrange: create fake API items
            var apiItems = new List<InrListItemVm>
            {
                new InrListItemVm { PublicId = Guid.NewGuid(), TestDate = System.DateTime.UtcNow, InrValue = 2.5m, Notes = "note", ReviewedByProvider = false }
            };

            var fakeRepo = new FakeInrRepository();
            var fakeService = new FakeInrService(apiItems);

            var vm = new InrListViewModel(fakeService, fakeRepo, null, null);

            // Act
            await vm.LoadInrLogs();

            // Assert: repository SaveRangeAsync was called and view model populated from repo
            Assert.True(fakeRepo.SaveRangeCalled, "Expected SaveRangeAsync to be called when repository is present");
            Assert.True(vm.InrLogs.Count > 0, "Expected InrLogs to be populated after load");
        }
    }
}
