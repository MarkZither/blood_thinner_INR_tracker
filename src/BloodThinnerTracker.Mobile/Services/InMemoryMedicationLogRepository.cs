using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiSdk.Models;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Mobile.Services
{
    public class InMemoryMedicationLogRepository : IMedicationLogRepository
    {
        private readonly List<MedicationLog> _logs = new();
        private int _nextId = 1;

        public InMemoryMedicationLogRepository()
        {
            // Seed empty
        }

        public Task<IEnumerable<MedicationLog>> GetByMedicationIdAsync(int medicationId)
        {
            var list = _logs.Where(l => l.MedicationId == medicationId).AsEnumerable();
            return Task.FromResult(list);
        }

        public Task AddAsync(MedicationLog log)
        {
            log.Id = _nextId++;
            log.CreatedAt = DateTime.UtcNow;
            _logs.Add(log);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(MedicationLog log)
        {
            var existing = _logs.FirstOrDefault(l => l.Id == log.Id);
            if (existing != null)
            {
                existing.ActualDosage = log.ActualDosage;
                existing.ActualTime = log.ActualTime;
                existing.Status = log.Status;
                existing.Notes = log.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            var existing = _logs.FirstOrDefault(l => l.Id == id);
            if (existing != null) _logs.Remove(existing);
            return Task.CompletedTask;
        }

        public Task SaveRangeAsync(IEnumerable<ApiSdk.Models.MedicationLogResponse> logs, CancellationToken cancellationToken = default)
        {
            if (logs == null) return Task.CompletedTask;

            foreach (var l in logs)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var publicId = l.PublicId ?? Guid.NewGuid();

                var existing = _logs.FirstOrDefault(x => x.PublicId == publicId);
                if (existing == null)
                {
                    var entity = new MedicationLog
                    {
                        Id = _nextId++,
                        PublicId = publicId,
                        MedicationId = 0, // In-memory mock cannot reliably map medication public id to internal id
                        ScheduledTime = l.ScheduledTime?.DateTime ?? DateTime.MinValue,
                        ActualTime = l.ActualTime?.DateTime,
                        Status = (MedicationLogStatus?)(l.Status ?? 0) ?? MedicationLogStatus.Scheduled,
                        ActualDosage = l.ActualDosage,
                        ActualDosageUnit = l.ActualDosageUnit,
                        ExpectedDosage = l.ExpectedDosage,
                        PatternDayNumber = l.PatternDayNumber,
                        Reason = l.Reason,
                        Notes = l.Notes,
                        SideEffects = l.SideEffects,
                        TakenWithFood = l.TakenWithFood,
                        TimeVarianceMinutes = l.TimeVarianceMinutes ?? 0,
                        CreatedAt = DateTime.UtcNow
                    };

                    _logs.Add(entity);
                }
                else
                {
                    existing.ScheduledTime = l.ScheduledTime?.DateTime ?? existing.ScheduledTime;
                    existing.ActualTime = l.ActualTime?.DateTime ?? existing.ActualTime;
                    existing.Status = (MedicationLogStatus?)(l.Status ?? (int)existing.Status) ?? existing.Status;
                    existing.ActualDosage = l.ActualDosage ?? existing.ActualDosage;
                    existing.ActualDosageUnit = l.ActualDosageUnit ?? existing.ActualDosageUnit;
                    existing.ExpectedDosage = l.ExpectedDosage ?? existing.ExpectedDosage;
                    existing.PatternDayNumber = l.PatternDayNumber ?? existing.PatternDayNumber;
                    existing.Reason = l.Reason ?? existing.Reason;
                    existing.Notes = l.Notes ?? existing.Notes;
                    existing.SideEffects = l.SideEffects ?? existing.SideEffects;
                    existing.TakenWithFood = l.TakenWithFood ?? existing.TakenWithFood;
                    existing.TimeVarianceMinutes = l.TimeVarianceMinutes ?? existing.TimeVarianceMinutes;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            return Task.CompletedTask;
        }
    }
}
