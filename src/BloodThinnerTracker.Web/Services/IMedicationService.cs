/*
 * BloodThinnerTracker.Web - Medication Service Interface
 * Licensed under MIT License. See LICENSE file in the project root.
 *
 * Service contract for medication management operations.
 */

using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Web.Services;

/// <summary>
/// Service interface for medication management operations.
/// </summary>
public interface IMedicationService
{
    /// <summary>
    /// Retrieves all medications for the authenticated user.
    /// </summary>
    Task<List<Medication>> GetMedicationsAsync();

    /// <summary>
    /// Retrieves a specific medication by ID.
    /// </summary>
    Task<Medication?> GetMedicationByIdAsync(string id);

    /// <summary>
    /// Creates a new medication.
    /// </summary>
    Task<Medication?> CreateMedicationAsync(Medication medication);

    /// <summary>
    /// Updates an existing medication.
    /// </summary>
    Task<bool> UpdateMedicationAsync(string id, Medication medication);

    /// <summary>
    /// Deactivates a medication (preserves history).
    /// </summary>
    Task<bool> DeactivateMedicationAsync(string id);

    /// <summary>
    /// Deletes a medication permanently.
    /// </summary>
    Task<bool> DeleteMedicationAsync(string id);

    /// <summary>
    /// Checks if a medication with the same name already exists.
    /// </summary>
    Task<bool> CheckDuplicateAsync(string name, string? excludeId = null);

    /// <summary>
    /// Gets medications that are due for refill based on threshold.
    /// </summary>
    Task<List<Medication>> GetMedicationsDueForRefillAsync();
}
