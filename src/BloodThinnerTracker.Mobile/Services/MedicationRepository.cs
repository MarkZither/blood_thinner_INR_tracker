using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiSdk.Models;
// Kiota parse node types no longer required after regenerating client with explicit numeric types
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services
{
    public interface IMedicationRepository
    {
        Task UpsertRangeAsync(IEnumerable<ApiSdk.Models.MedicationResponse> meds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active medications from the local database.
        /// </summary>
        Task<IReadOnlyList<Medication>> GetActiveMedicationsAsync(CancellationToken cancellationToken = default);
    }

    public class MedicationRepository : IMedicationRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly BloodThinnerTracker.Data.Shared.ICurrentUserService? _currentUserService;
        private readonly Microsoft.Extensions.Logging.ILogger<MedicationRepository> _logger;

        public MedicationRepository(ApplicationDbContext db, BloodThinnerTracker.Data.Shared.ICurrentUserService? currentUserService = null, Microsoft.Extensions.Logging.ILogger<MedicationRepository>? logger = null)
        {
            _db = db;
            _currentUserService = currentUserService;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MedicationRepository>.Instance;
        }

        public async Task UpsertRangeAsync(IEnumerable<ApiSdk.Models.MedicationResponse> meds, CancellationToken cancellationToken = default)
        {
            if (meds == null) return;

            int skippedNewMeds = 0;

            foreach (var m in meds)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var publicId = m.PublicId ?? Guid.Empty;
                var existing = await _db.Medications.FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken).ConfigureAwait(false);

                // Regenerated client now provides strongly-typed numeric properties (decimal/int),
                // so we can use them directly and avoid fragile runtime parsing.

                if (existing == null)
                {
                    var parsedDosage = m.Dosage;
                    if (!parsedDosage.HasValue || parsedDosage.Value <= 0)
                    {
                        // Dosage is medically required; do not persist an invalid medication.
                        _logger.LogWarning("MedicationRepository: skipping medication {PublicId} because dosage is missing or invalid", publicId);
                        skippedNewMeds++;
                        continue;
                    }

                    var entity = new Medication
                    {
                        PublicId = publicId,
                        Name = m.Name ?? string.Empty,
                        GenericName = m.GenericName,
                        BrandName = m.BrandName,
                        Type = (MedicationType?)(m.Type ?? 0) ?? MedicationType.Other,
                        Dosage = parsedDosage.Value,
                        DosageUnit = m.DosageUnit ?? "mg",
                        Form = m.Form,
                        Color = m.Color,
                        Shape = m.Shape,
                        Imprint = m.Imprint,
                        IsBloodThinner = m.IsBloodThinner ?? true,
                        Contraindications = m.Contraindications,
                        StorageInstructions = m.StorageInstructions,
                        MaxDailyDose = m.MaxDailyDose ?? 0m,
                        MinHoursBetweenDoses = m.MinHoursBetweenDoses ?? 0,
                        RequiresINRMonitoring = m.RequiresINRMonitoring ?? false,
                        INRTargetMin = null,
                        INRTargetMax = null,
                        Frequency = (MedicationFrequency?)(m.Frequency ?? 0) ?? MedicationFrequency.OnceDaily,
                        CustomFrequency = m.CustomFrequency,
                        ScheduledTimes = m.ScheduledTimes,
                        StartDate = m.StartDate?.DateTime ?? DateTime.UtcNow.Date,
                        EndDate = m.EndDate?.DateTime,
                        IsActive = m.IsActive ?? true,
                        PrescribedBy = m.PrescribedBy,
                        PrescriptionDate = m.PrescriptionDate?.DateTime,
                        Pharmacy = m.Pharmacy,
                        PrescriptionNumber = m.PrescriptionNumber,
                        Instructions = m.Instructions,
                        FoodInteractions = m.FoodInteractions,
                        DrugInteractions = m.DrugInteractions,
                        SideEffects = m.SideEffects,
                        RemindersEnabled = m.RemindersEnabled ?? true,
                        ReminderMinutes = m.ReminderMinutes ?? 0,
                        Notes = m.Notes
                    };

                    _db.Medications.Add(entity);
                }
                else
                {
                    existing.Name = m.Name ?? existing.Name;
                    existing.GenericName = m.GenericName ?? existing.GenericName;
                    existing.BrandName = m.BrandName ?? existing.BrandName;
                    existing.Type = (MedicationType?)(m.Type ?? (int)existing.Type) ?? existing.Type;
                    existing.Dosage = m.Dosage ?? existing.Dosage;
                    existing.DosageUnit = m.DosageUnit ?? existing.DosageUnit;
                    existing.Form = m.Form ?? existing.Form;
                    existing.Color = m.Color ?? existing.Color;
                    existing.Shape = m.Shape ?? existing.Shape;
                    existing.Imprint = m.Imprint ?? existing.Imprint;
                    existing.IsBloodThinner = m.IsBloodThinner ?? existing.IsBloodThinner;
                    existing.Contraindications = m.Contraindications ?? existing.Contraindications;
                    existing.StorageInstructions = m.StorageInstructions ?? existing.StorageInstructions;
                    existing.MaxDailyDose = m.MaxDailyDose ?? existing.MaxDailyDose;
                    existing.MinHoursBetweenDoses = m.MinHoursBetweenDoses ?? existing.MinHoursBetweenDoses;
                    existing.RequiresINRMonitoring = m.RequiresINRMonitoring ?? existing.RequiresINRMonitoring;
                    existing.Frequency = (MedicationFrequency?)(m.Frequency ?? (int)existing.Frequency) ?? existing.Frequency;
                    existing.CustomFrequency = m.CustomFrequency ?? existing.CustomFrequency;
                    existing.ScheduledTimes = m.ScheduledTimes ?? existing.ScheduledTimes;
                    existing.StartDate = m.StartDate?.DateTime ?? existing.StartDate;
                    existing.EndDate = m.EndDate?.DateTime ?? existing.EndDate;
                    existing.IsActive = m.IsActive ?? existing.IsActive;
                    existing.PrescribedBy = m.PrescribedBy ?? existing.PrescribedBy;
                    existing.PrescriptionDate = m.PrescriptionDate?.DateTime ?? existing.PrescriptionDate;
                    existing.Pharmacy = m.Pharmacy ?? existing.Pharmacy;
                    existing.PrescriptionNumber = m.PrescriptionNumber ?? existing.PrescriptionNumber;
                    existing.Instructions = m.Instructions ?? existing.Instructions;
                    existing.FoodInteractions = m.FoodInteractions ?? existing.FoodInteractions;
                    existing.DrugInteractions = m.DrugInteractions ?? existing.DrugInteractions;
                    existing.SideEffects = m.SideEffects ?? existing.SideEffects;
                    existing.RemindersEnabled = m.RemindersEnabled ?? existing.RemindersEnabled;
                    existing.ReminderMinutes = m.ReminderMinutes ?? existing.ReminderMinutes;
                    existing.Notes = m.Notes ?? existing.Notes;

                        _db.Medications.Update(existing);
                }
            }

                if (skippedNewMeds > 0)
                {
                    throw new InvalidOperationException($"MedicationRepository: {skippedNewMeds} medications were missing required fields (dosage) and were not saved.");
                }

                // Persist with current-user handling similar to InrRepository
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
                throw new InvalidOperationException("Saving medications failed. Ensure ApplicationDbContext has the required current-user services registered in DI (the mobile app must supply a current user when persisting medical entities).", ex);
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Medication>> GetActiveMedicationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var medications = await _db.Medications
                    .Where(m => m.IsActive
                        && m.StartDate.Date <= today
                        && (!m.EndDate.HasValue || m.EndDate.Value.Date >= today))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("MedicationRepository: Retrieved {Count} active medications", medications.Count);
                return medications;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MedicationRepository: Failed to get active medications");
                return Array.Empty<Medication>();
            }
        }
    }
}
