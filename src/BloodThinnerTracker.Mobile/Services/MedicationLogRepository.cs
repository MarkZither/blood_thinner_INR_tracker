using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiSdk.Models;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodThinnerTracker.Mobile.Services
{
    public interface IMedicationLogRepository
    {
        Task SaveRangeAsync(IEnumerable<ApiSdk.Models.MedicationLogResponse> logs, CancellationToken cancellationToken = default);
    }

    public class MedicationLogRepository : IMedicationLogRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly BloodThinnerTracker.Data.Shared.ICurrentUserService? _currentUserService;

        public MedicationLogRepository(ApplicationDbContext db, BloodThinnerTracker.Data.Shared.ICurrentUserService? currentUserService = null)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        public async Task SaveRangeAsync(IEnumerable<ApiSdk.Models.MedicationLogResponse> logs, CancellationToken cancellationToken = default)
        {
            if (logs == null) return;

            // Regenerated client provides strongly-typed numeric properties (decimal/int),
            // so use them directly instead of parsing UntypedNode.

            foreach (var l in logs)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Find related medication by public id
                var medPublicId = l.MedicationPublicId ?? Guid.Empty;
                var medication = await _db.Medications.FirstOrDefaultAsync(m => m.PublicId == medPublicId, cancellationToken).ConfigureAwait(false);
                if (medication == null)
                {
                    // If no medication exists for this log, skip it (we cached earlier)
                    continue;
                }

                var publicId = l.PublicId ?? Guid.Empty;
                var existing = await _db.MedicationLogs.FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken).ConfigureAwait(false);

                if (existing == null)
                {
                    var entity = new MedicationLog
                    {
                        PublicId = publicId,
                        MedicationId = medication.Id,
                        ScheduledTime = l.ScheduledTime?.DateTime ?? DateTime.MinValue,
                        ActualTime = l.ActualTime?.DateTime,
                        Status = (BloodThinnerTracker.Shared.Models.MedicationLogStatus?)(l.Status ?? 0) ?? BloodThinnerTracker.Shared.Models.MedicationLogStatus.Scheduled,
                        ActualDosage = l.ActualDosage,
                        ActualDosageUnit = l.ActualDosageUnit,
                        ExpectedDosage = l.ExpectedDosage,
                        PatternDayNumber = l.PatternDayNumber,
                        Reason = l.Reason,
                        Notes = l.Notes,
                        SideEffects = l.SideEffects,
                        TakenWithFood = l.TakenWithFood,
                        TimeVarianceMinutes = l.TimeVarianceMinutes ?? 0,
                        ConfirmedByProvider = false
                    };

                    _db.MedicationLogs.Add(entity);
                }
                else
                {
                    existing.MedicationId = medication.Id;
                    existing.ScheduledTime = l.ScheduledTime?.DateTime ?? existing.ScheduledTime;
                    existing.ActualTime = l.ActualTime?.DateTime ?? existing.ActualTime;
                    existing.Status = (BloodThinnerTracker.Shared.Models.MedicationLogStatus?)(l.Status ?? (int)existing.Status) ?? existing.Status;
                    existing.ActualDosage = l.ActualDosage ?? existing.ActualDosage;
                    existing.ActualDosageUnit = l.ActualDosageUnit ?? existing.ActualDosageUnit;
                    existing.ExpectedDosage = l.ExpectedDosage ?? existing.ExpectedDosage;
                    existing.PatternDayNumber = l.PatternDayNumber ?? existing.PatternDayNumber;
                    existing.Reason = l.Reason ?? existing.Reason;
                    existing.Notes = l.Notes ?? existing.Notes;
                    existing.SideEffects = l.SideEffects ?? existing.SideEffects;
                    existing.TakenWithFood = l.TakenWithFood ?? existing.TakenWithFood;
                    existing.TimeVarianceMinutes = l.TimeVarianceMinutes ?? existing.TimeVarianceMinutes;

                    _db.MedicationLogs.Update(existing);
                }
            }

            try
            {
                int? currentUserId = null;
                if (_currentUserService != null) currentUserId = _currentUserService.GetCurrentUserId();

                if (!currentUserId.HasValue)
                {
                    var localUser = await _db.Users.FirstOrDefaultAsync(u => u.AuthProvider == "Local" && u.Email == "mobile@local", cancellationToken).ConfigureAwait(false);
                    if (localUser == null)
                    {
                        localUser = new User
                        {
                            PublicId = Guid.NewGuid(),
                            Email = "mobile@local",
                            Name = "Local Device User",
                            AuthProvider = "Local",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        _db.Users.Add(localUser);
                        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    }

                    currentUserId = localUser.Id;
                }

                if (currentUserId.HasValue) _db.CurrentUserId = currentUserId.Value;

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("Saving medication logs failed. Ensure ApplicationDbContext has the required current-user services registered in DI (the mobile app must supply a current user when persisting medical entities).", ex);
            }
        }
    }
}
