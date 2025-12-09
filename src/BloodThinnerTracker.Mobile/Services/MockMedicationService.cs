using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Lightweight in-memory mock implementation of IMedicationService for local/dev testing.
    /// Not intended for production use.
    /// </summary>
    public interface IMedicationService
    {
        Task<List<Medication>> GetMedicationsAsync();
        Task<Medication?> GetMedicationByIdAsync(string id);
        Task<Medication?> CreateMedicationAsync(Medication medication);
        Task<bool> UpdateMedicationAsync(string id, Medication medication);
        Task<bool> DeactivateMedicationAsync(string id);
        Task<bool> DeleteMedicationAsync(string id);
        Task<bool> CheckDuplicateAsync(string name, string? excludeId = null);
        Task<List<Medication>> GetMedicationsDueForRefillAsync();
    }

    public class MockMedicationService : IMedicationService
    {
        private readonly List<Medication> _store = new();
        private int _nextId = 1;

        public MockMedicationService()
        {
            // Seed a couple of medications for developer convenience
            _store.Add(new Medication
            {
                Id = _nextId++,
                PublicId = Guid.NewGuid(),
                Name = "Warfarin",
                Dosage = 5m,
                DosageUnit = "mg",
                StartDate = DateTime.UtcNow.Date.AddDays(-30),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

            _store.Add(new Medication
            {
                Id = _nextId++,
                PublicId = Guid.NewGuid(),
                Name = "Apixaban",
                Dosage = 2.5m,
                DosageUnit = "mg",
                StartDate = DateTime.UtcNow.Date.AddDays(-7),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        public Task<List<Medication>> GetMedicationsAsync()
        {
            // Return a shallow copy to avoid accidental mutation
            return Task.FromResult(_store.Select(m => Clone(m)).ToList());
        }

        public Task<Medication?> GetMedicationByIdAsync(string id)
        {
            if (Guid.TryParse(id, out var gid))
            {
                var med = _store.FirstOrDefault(m => m.PublicId == gid);
                return Task.FromResult(med != null ? Clone(med) : null);
            }

            return Task.FromResult<Medication?>(null);
        }

        public Task<Medication?> CreateMedicationAsync(Medication medication)
        {
            if (medication == null) return Task.FromResult<Medication?>(null);
            var m = Clone(medication);
            m.Id = _nextId++;
            m.PublicId = Guid.NewGuid();
            m.CreatedAt = DateTime.UtcNow;
            _store.Add(m);
            return Task.FromResult<Medication?>(Clone(m));
        }

        public Task<bool> UpdateMedicationAsync(string id, Medication medication)
        {
            if (!Guid.TryParse(id, out var gid)) return Task.FromResult(false);
            var existing = _store.FirstOrDefault(m => m.PublicId == gid);
            if (existing == null) return Task.FromResult(false);

            // Update allowed fields
            existing.Name = medication.Name;
            existing.Dosage = medication.Dosage;
            existing.DosageUnit = medication.DosageUnit;
            existing.IsActive = medication.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> DeactivateMedicationAsync(string id)
        {
            if (!Guid.TryParse(id, out var gid)) return Task.FromResult(false);
            var existing = _store.FirstOrDefault(m => m.PublicId == gid);
            if (existing == null) return Task.FromResult(false);
            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteMedicationAsync(string id)
        {
            if (!Guid.TryParse(id, out var gid)) return Task.FromResult(false);
            var existing = _store.FirstOrDefault(m => m.PublicId == gid);
            if (existing == null) return Task.FromResult(false);
            _store.Remove(existing);
            return Task.FromResult(true);
        }

        public Task<bool> CheckDuplicateAsync(string name, string? excludeId = null)
        {
            int? excludeInt = null;
            if (!string.IsNullOrEmpty(excludeId) && int.TryParse(excludeId, out var parsed)) excludeInt = parsed;

            var exists = _store.Any(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase) && (!excludeInt.HasValue || m.Id != excludeInt.Value));
            return Task.FromResult(exists);
        }

        public Task<List<Medication>> GetMedicationsDueForRefillAsync()
        {
            // No refill metadata in mock - return empty list
            return Task.FromResult(new List<Medication>());
        }

        private static Medication Clone(Medication src)
        {
            return new Medication
            {
                Id = src.Id,
                PublicId = src.PublicId,
                Name = src.Name,
                Dosage = src.Dosage,
                DosageUnit = src.DosageUnit,
                StartDate = src.StartDate,
                EndDate = src.EndDate,
                CreatedAt = src.CreatedAt,
                UpdatedAt = src.UpdatedAt,
                IsActive = src.IsActive
            };
        }
    }
}
