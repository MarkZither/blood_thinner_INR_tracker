using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiSdk.Models;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Minimal facade over the generated Kiota API client (or a simple HttpClient-based fallback).
    /// Provides methods the mobile sync service needs: medications, medication logs, and INR tests.
    /// This facade allows swapping to the full generated client later without touching the sync logic.
    /// </summary>
    public interface IApiClientFacade
    {
        Task<List<MedicationResponse>?> GetMedicationsAsync(CancellationToken cancellationToken = default);

        Task<List<global::ApiSdk.Models.MedicationLogResponse>?> GetMedicationLogsAsync(Guid medicationPublicId, CancellationToken cancellationToken = default);

        Task<List<global::ApiSdk.Models.INRTestResponse>?> GetRecentInrTestsAsync(int take = 50, CancellationToken cancellationToken = default);
    }
}
