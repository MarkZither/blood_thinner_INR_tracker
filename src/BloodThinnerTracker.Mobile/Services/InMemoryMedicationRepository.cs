using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiSdk.Models;
using System.Threading;
using BloodThinnerTracker.Shared.Models;
using BloodThinnerTracker.Mobile.ViewModels;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Simple in-memory medication repository for mock mode.
    /// Provides read operations used by the mobile UI.
    /// </summary>
    public class InMemoryMedicationRepository : IMedicationRepository
    {
        private readonly List<Medication> _meds = new();

        public InMemoryMedicationRepository()
        {
            // Seed sample medications
            _meds.Add(new Medication { Id = 1, PublicId = Guid.NewGuid(), Name = "Warfarin", Dosage = 5m, DosageUnit = "mg", StartDate = DateTime.UtcNow.Date.AddDays(-30), CreatedAt = DateTime.UtcNow, IsActive = true });
            _meds.Add(new Medication { Id = 2, PublicId = Guid.NewGuid(), Name = "Apixaban", Dosage = 2.5m, DosageUnit = "mg", StartDate = DateTime.UtcNow.Date.AddDays(-7), CreatedAt = DateTime.UtcNow, IsActive = true });
        }

        public Task<IEnumerable<Medication>> GetAllAsync()
        {
            return Task.FromResult(_meds.AsEnumerable());
        }

        public Task<Medication?> GetByPublicIdAsync(Guid publicId)
        {
            var med = _meds.FirstOrDefault(m => m.PublicId == publicId);
            return Task.FromResult(med);
        }

        public Task AddAsync(Medication medication)
        {
            medication.Id = _meds.Count > 0 ? _meds.Max(m => m.Id) + 1 : 1;
            medication.PublicId = Guid.NewGuid();
            medication.CreatedAt = DateTime.UtcNow;
            _meds.Add(medication);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Medication medication)
        {
            var existing = _meds.FirstOrDefault(m => m.Id == medication.Id);
            if (existing != null)
            {
                existing.Name = medication.Name;
                existing.Dosage = medication.Dosage;
                existing.DosageUnit = medication.DosageUnit;
                existing.IsActive = medication.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            var existing = _meds.FirstOrDefault(m => m.Id == id);
            if (existing != null) _meds.Remove(existing);
            return Task.CompletedTask;
        }

        public Task UpsertRangeAsync(IEnumerable<ApiSdk.Models.MedicationResponse> meds, CancellationToken cancellationToken = default)
        {
            if (meds == null) return Task.CompletedTask;

            foreach (var m in meds)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var publicId = m.PublicId ?? Guid.Empty;
                var existing = _meds.FirstOrDefault(x => x.PublicId == publicId);

                if (existing == null)
                {
                    var dosage = m.Dosage ?? 0m;
                    if (dosage <= 0)
                    {
                        // Skip invalid medication entries in mock mode
                        continue;
                    }

                    var entity = new Medication
                    {
                        Id = _meds.Count > 0 ? _meds.Max(x => x.Id) + 1 : 1,
                        PublicId = publicId == Guid.Empty ? Guid.NewGuid() : publicId,
                        Name = m.Name ?? string.Empty,
                        Dosage = dosage,
                        DosageUnit = m.DosageUnit ?? "mg",
                        StartDate = m.StartDate?.DateTime ?? DateTime.UtcNow.Date,
                        IsActive = m.IsActive ?? true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _meds.Add(entity);
                }
                else
                {
                    existing.Name = m.Name ?? existing.Name;
                    existing.Dosage = m.Dosage ?? existing.Dosage;
                    existing.DosageUnit = m.DosageUnit ?? existing.DosageUnit;
                    existing.IsActive = m.IsActive ?? existing.IsActive;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            return Task.CompletedTask;
        }
    }
}
