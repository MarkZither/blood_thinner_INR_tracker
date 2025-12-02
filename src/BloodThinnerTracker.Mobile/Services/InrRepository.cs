using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Data.Shared;
using BloodThinnerTracker.Shared.Models;
using BloodThinnerTracker.Mobile.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Repository abstraction for INR tests. Provides a small set of read/write
    /// operations against the canonical local SQLite DB (`ApplicationDbContext`).
    ///
    /// For now the app primarily uses read operations (offline-first). Write
    /// methods are implemented to allow future sync code to persist API-fetched
    /// results into the canonical DB. Note: saving requires the application's
    /// normal current-user plumbing to be registered (ApplicationDbContext's
    /// validation expects a current user for medical entities).
    /// </summary>
    public interface IInrRepository
    {
        Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 10);

        /// <summary>
        /// Upserts a batch of INR items into the local DB. Implementations should
        /// map the view-model into the canonical `INRTest` entity and call SaveChanges.
        /// This is intended for background sync usage.
        /// </summary>
        Task SaveRangeAsync(IEnumerable<InrListItemVm> items);
    }

    public class InrRepository : IInrRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly BloodThinnerTracker.Data.Shared.ICurrentUserService? _currentUserService;

        public InrRepository(ApplicationDbContext db, BloodThinnerTracker.Data.Shared.ICurrentUserService? currentUserService = null)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 10)
        {
            var query = _db.INRTests
                .Where(QueryFilters.NotDeleted<INRTest>())
                .OrderByDescending(t => t.TestDate)
                .Take(count)
                .Select(t => new InrListItemVm
                {
                    PublicId = t.PublicId,
                    TestDate = t.TestDate,
                    InrValue = t.INRValue,
                    Notes = t.Notes,
                    ReviewedByProvider = t.ReviewedByProvider
                });

            var list = await query.ToListAsync();
            return list;
        }

        public async Task SaveRangeAsync(IEnumerable<InrListItemVm> items)
        {
            if (items == null) return;

            foreach (var vm in items)
            {
                var existing = await _db.INRTests.FirstOrDefaultAsync(x => x.PublicId == vm.PublicId);
                if (existing == null)
                {
                    var entity = new INRTest
                    {
                        PublicId = vm.PublicId,
                        TestDate = vm.TestDate,
                        INRValue = vm.InrValue,
                        Notes = vm.Notes,
                        ReviewedByProvider = vm.ReviewedByProvider,
                        IsDeleted = false
                    };

                    _db.INRTests.Add(entity);
                }
                else
                {
                    existing.TestDate = vm.TestDate;
                    existing.INRValue = vm.InrValue;
                    existing.Notes = vm.Notes;
                    existing.ReviewedByProvider = vm.ReviewedByProvider;
                    existing.IsDeleted = false;
                    _db.INRTests.Update(existing);
                }
            }

            // Ensure ApplicationDbContext.CurrentUserId is set for mobile when available
            try
            {
                if (_currentUserService != null)
                {
                    var currentUserId = _currentUserService.GetCurrentUserId();
                    if (currentUserId.HasValue)
                    {
                        _db.CurrentUserId = currentUserId.Value;
                    }
                }

                await _db.SaveChangesAsync();
            }
            catch (System.InvalidOperationException ex)
            {
                throw new System.InvalidOperationException("Saving INR items failed. Ensure ApplicationDbContext has the required current-user services registered in DI (the mobile app must supply a current user when persisting medical entities).", ex);
            }
        }
    }
}
