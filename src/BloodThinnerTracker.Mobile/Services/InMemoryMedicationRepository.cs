using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    /// Seeds sample medications with reminders scheduled in the next 15 minutes for testing.
    /// </summary>
    public class InMemoryMedicationRepository : IMedicationRepository
    {
        private readonly List<Medication> _meds = new();

        public InMemoryMedicationRepository()
        {
            // Seed sample medications with reminders in the next 15 minutes for testing
            SeedMedicationsWithUpcomingReminders();
        }

        /// <summary>
        /// Seeds medications with scheduled times that will trigger reminders in the next 15 minutes.
        /// This allows testing notification scheduling without waiting.
        /// </summary>
        private void SeedMedicationsWithUpcomingReminders()
        {
            var now = DateTime.Now;
            
            // Create scheduled times for the next 15 minutes (at 3, 6, 9, 12, 15 minute marks)
            var upcomingTimes = new List<string>();
            for (int i = 3; i <= 15; i += 3)
            {
                var reminderTime = now.AddMinutes(i);
                upcomingTimes.Add(reminderTime.ToString("HH:mm"));
            }

            // Medication 1: Warfarin - Daily with 2 reminders in next 15 min
            _meds.Add(new Medication
            {
                Id = 1,
                PublicId = Guid.NewGuid(),
                Name = "Warfarin",
                GenericName = "Warfarin Sodium",
                BrandName = "Coumadin",
                Type = MedicationType.VitKAntagonist,
                Dosage = 5m,
                DosageUnit = "mg",
                Form = "Tablet",
                Color = "Pink",
                IsBloodThinner = true,
                RequiresINRMonitoring = true,
                INRTargetMin = 2.0m,
                INRTargetMax = 3.0m,
                Frequency = MedicationFrequency.OnceDaily,
                ScheduledTimes = JsonSerializer.Serialize(new[] { upcomingTimes[0], upcomingTimes[2] }), // 3 min and 9 min from now
                StartDate = DateTime.UtcNow.Date.AddDays(-30),
                IsActive = true,
                RemindersEnabled = true,
                ReminderMinutes = 0, // Remind at exact time
                CreatedAt = DateTime.UtcNow,
                Instructions = "Take with food. Avoid vitamin K-rich foods.",
                Notes = "Mock medication for testing reminders"
            });

            // Medication 2: Apixaban - Twice daily with 1 reminder in next 15 min
            _meds.Add(new Medication
            {
                Id = 2,
                PublicId = Guid.NewGuid(),
                Name = "Apixaban",
                GenericName = "Apixaban",
                BrandName = "Eliquis",
                Type = MedicationType.DOAC,
                Dosage = 2.5m,
                DosageUnit = "mg",
                Form = "Tablet",
                Color = "Yellow",
                IsBloodThinner = true,
                RequiresINRMonitoring = false,
                Frequency = MedicationFrequency.TwiceDaily,
                ScheduledTimes = JsonSerializer.Serialize(new[] { upcomingTimes[1] }), // 6 min from now
                StartDate = DateTime.UtcNow.Date.AddDays(-7),
                IsActive = true,
                RemindersEnabled = true,
                ReminderMinutes = 0,
                CreatedAt = DateTime.UtcNow,
                Instructions = "Take with or without food.",
                Notes = "Mock medication for testing reminders"
            });

            // Medication 3: Aspirin - Daily with reminder at 12 min mark
            _meds.Add(new Medication
            {
                Id = 3,
                PublicId = Guid.NewGuid(),
                Name = "Aspirin",
                GenericName = "Acetylsalicylic Acid",
                BrandName = "Bayer",
                Type = MedicationType.Antiplatelet,
                Dosage = 81m,
                DosageUnit = "mg",
                Form = "Tablet",
                Color = "Orange",
                IsBloodThinner = false,
                RequiresINRMonitoring = false,
                Frequency = MedicationFrequency.OnceDaily,
                ScheduledTimes = JsonSerializer.Serialize(new[] { upcomingTimes[3] }), // 12 min from now
                StartDate = DateTime.UtcNow.Date.AddDays(-60),
                IsActive = true,
                RemindersEnabled = true,
                ReminderMinutes = 0,
                CreatedAt = DateTime.UtcNow,
                Instructions = "Take with food to reduce stomach upset.",
                Notes = "Low-dose aspirin for cardiovascular protection"
            });

            // Medication 4: Rivaroxaban - Daily with reminder at 15 min mark
            _meds.Add(new Medication
            {
                Id = 4,
                PublicId = Guid.NewGuid(),
                Name = "Rivaroxaban",
                GenericName = "Rivaroxaban",
                BrandName = "Xarelto",
                Type = MedicationType.DOAC,
                Dosage = 20m,
                DosageUnit = "mg",
                Form = "Tablet",
                Color = "Red",
                IsBloodThinner = true,
                RequiresINRMonitoring = false,
                Frequency = MedicationFrequency.OnceDaily,
                ScheduledTimes = JsonSerializer.Serialize(new[] { upcomingTimes[4] }), // 15 min from now
                StartDate = DateTime.UtcNow.Date.AddDays(-14),
                IsActive = true,
                RemindersEnabled = true,
                ReminderMinutes = 0,
                CreatedAt = DateTime.UtcNow,
                Instructions = "Take with evening meal.",
                Notes = "Direct oral anticoagulant (DOAC)"
            });

            // Medication 5: Inactive medication (should not trigger reminders)
            _meds.Add(new Medication
            {
                Id = 5,
                PublicId = Guid.NewGuid(),
                Name = "Discontinued Med",
                Type = MedicationType.Other,
                Dosage = 10m,
                DosageUnit = "mg",
                Frequency = MedicationFrequency.OnceDaily,
                ScheduledTimes = JsonSerializer.Serialize(new[] { now.AddMinutes(5).ToString("HH:mm") }),
                StartDate = DateTime.UtcNow.Date.AddDays(-90),
                EndDate = DateTime.UtcNow.Date.AddDays(-30), // Ended 30 days ago
                IsActive = false,
                RemindersEnabled = true, // Even though enabled, should not fire (inactive)
                ReminderMinutes = 0,
                CreatedAt = DateTime.UtcNow,
                Notes = "This medication was discontinued and should NOT trigger reminders"
            });
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

        /// <inheritdoc />
        public Task<IReadOnlyList<Medication>> GetActiveMedicationsAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var active = _meds
                .Where(m => m.IsActive
                    && m.StartDate.Date <= today
                    && (!m.EndDate.HasValue || m.EndDate.Value.Date >= today))
                .ToList();
            return Task.FromResult<IReadOnlyList<Medication>>(active);
        }
    }
}
