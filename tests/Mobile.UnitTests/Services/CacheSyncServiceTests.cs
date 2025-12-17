using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using BloodThinnerTracker.Mobile.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace BloodThinnerTracker.Mobile.UnitTests.Services
{
    public class CacheSyncServiceTests
    {
        [Fact]
        public async Task FetchAndUpsertAsync_PersistsInrTests_WhenApiReturnsResults()
        {
            var api = new Mock<IApiClientFacade>();
            var inrRepo = new Mock<IInrRepository>();
            var cache = new Mock<ICacheService>();
            var medRepo = new Mock<IMedicationRepository>();
            var medLogRepo = new Mock<IMedicationLogRepository>();

            // Setup API to return one INR test
            var inr = new List<global::ApiSdk.Models.INRTestResponse>
            {
                new global::ApiSdk.Models.INRTestResponse { PublicId = Guid.NewGuid(), TestDate = DateTimeOffset.UtcNow }
            };
            api.Setup(a => a.GetRecentInrTestsAsync(50, It.IsAny<CancellationToken>())).ReturnsAsync(inr);
            api.Setup(a => a.GetMedicationsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<global::ApiSdk.Models.MedicationResponse>());

            var svc = new CacheSyncService(api.Object, inrRepo.Object, cache.Object, medRepo.Object, medLogRepo.Object, Microsoft.Extensions.Options.Options.Create(new Config.SyncOptions()), new NullLogger<CacheSyncService>());

            await svc.FetchAndUpsertAsync(CancellationToken.None);

            inrRepo.Verify(r => r.SaveRangeAsync(It.IsAny<IEnumerable<ViewModels.InrListItemVm>>()), Times.Once);
        }

        [Fact]
        public async Task FetchAndUpsertAsync_UpsertsMedications_WhenApiReturnsMedications()
        {
            var api = new Mock<IApiClientFacade>();
            var inrRepo = new Mock<IInrRepository>();
            var cache = new Mock<ICacheService>();
            var medRepo = new Mock<IMedicationRepository>();
            var medLogRepo = new Mock<IMedicationLogRepository>();

            var meds = new List<global::ApiSdk.Models.MedicationResponse>
            {
                new global::ApiSdk.Models.MedicationResponse { PublicId = Guid.NewGuid(), Name = "Test" }
            };

            api.Setup(a => a.GetRecentInrTestsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((List<global::ApiSdk.Models.INRTestResponse>?)null);
            api.Setup(a => a.GetMedicationsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(meds);
            api.Setup(a => a.GetMedicationLogsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<global::ApiSdk.Models.MedicationLogResponse>());

            var svc = new CacheSyncService(api.Object, inrRepo.Object, cache.Object, medRepo.Object, medLogRepo.Object, Microsoft.Extensions.Options.Options.Create(new Config.SyncOptions()), new NullLogger<CacheSyncService>());

            await svc.FetchAndUpsertAsync(CancellationToken.None);

            medRepo.Verify(r => r.UpsertRangeAsync(meds, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FetchAndUpsertAsync_PersistsMedicationLogs_WhenApiReturnsLogs()
        {
            var api = new Mock<IApiClientFacade>();
            var inrRepo = new Mock<IInrRepository>();
            var cache = new Mock<ICacheService>();
            var medRepo = new Mock<IMedicationRepository>();
            var medLogRepo = new Mock<IMedicationLogRepository>();

            var medId = Guid.NewGuid();
            var meds = new List<global::ApiSdk.Models.MedicationResponse>
            {
                new global::ApiSdk.Models.MedicationResponse { PublicId = medId, Name = "Test" }
            };

            var logs = new List<global::ApiSdk.Models.MedicationLogResponse>
            {
                new global::ApiSdk.Models.MedicationLogResponse { PublicId = Guid.NewGuid(), MedicationPublicId = medId }
            };

            api.Setup(a => a.GetRecentInrTestsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((List<global::ApiSdk.Models.INRTestResponse>?)null);
            api.Setup(a => a.GetMedicationsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(meds);
            api.Setup(a => a.GetMedicationLogsAsync(medId, It.IsAny<CancellationToken>())).ReturnsAsync(logs);

            var svc = new CacheSyncService(api.Object, inrRepo.Object, cache.Object, medRepo.Object, medLogRepo.Object, Microsoft.Extensions.Options.Options.Create(new Config.SyncOptions()), new NullLogger<CacheSyncService>());

            await svc.FetchAndUpsertAsync(CancellationToken.None);

            medLogRepo.Verify(r => r.SaveRangeAsync(It.Is<IEnumerable<global::ApiSdk.Models.MedicationLogResponse>>(c => c != null), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
