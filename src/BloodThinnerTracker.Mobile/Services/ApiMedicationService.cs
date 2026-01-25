using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Lightweight API-backed implementation of IMedicationService for mobile.
    /// Uses <see cref="IApiClientFacade"/> to fetch medication data from the server.
    /// Write operations are currently not supported in this minimal implementation.
    /// </summary>
    public class ApiMedicationService : IMedicationService
    {
        private readonly IApiClientFacade _api;
        private readonly ILogger<ApiMedicationService>? _logger;

        public ApiMedicationService(IApiClientFacade api, ILogger<ApiMedicationService>? logger = null)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _logger = logger;
        }

        public async Task<List<Medication>> GetMedicationsAsync()
        {
            try
            {
                var list = await _api.GetMedicationsAsync().ConfigureAwait(false);
                if (list == null) return new List<Medication>();

                // Map API model to shared Medication model (best-effort mapping)
                return list.Select(m => new Medication
                {
                    Id = 0,
                    PublicId = m.PublicId ?? Guid.Empty,
                    Name = m.Name ?? string.Empty,
                    Dosage = m.Dosage ?? 0m,
                    DosageUnit = m.DosageUnit ?? "mg",
                    StartDate = m.StartDate?.DateTime ?? DateTime.UtcNow.Date,
                    EndDate = m.EndDate?.DateTime,
                    IsActive = m.IsActive ?? true,
                    CreatedAt = m.CreatedAt?.DateTime ?? DateTime.UtcNow
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "ApiMedicationService: failed to fetch medications");
                return new List<Medication>();
            }
        }

        public Task<Medication?> GetMedicationByIdAsync(string id)
        {
            // Not implemented for minimal read-only mobile client - caller should use GetMedicationsAsync
            throw new NotSupportedException("GetMedicationByIdAsync is not supported by ApiMedicationService");
        }

        public Task<Medication?> CreateMedicationAsync(Medication medication) => throw new NotSupportedException();
        public Task<bool> UpdateMedicationAsync(string id, Medication medication) => throw new NotSupportedException();
        public Task<bool> DeactivateMedicationAsync(string id) => throw new NotSupportedException();
        public Task<bool> DeleteMedicationAsync(string id) => throw new NotSupportedException();
        public Task<bool> CheckDuplicateAsync(string name, string? excludeId = null) => throw new NotSupportedException();
        public Task<List<Medication>> GetMedicationsDueForRefillAsync() => Task.FromResult(new List<Medication>());
    }
}
