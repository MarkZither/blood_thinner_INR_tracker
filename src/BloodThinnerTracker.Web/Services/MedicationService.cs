/*
 * BloodThinnerTracker.Web - Medication Service Implementation
 * Licensed under MIT License. See LICENSE file in the project root.
 *
 * HttpClient-based service for medication management operations.
 */

using BloodThinnerTracker.Shared.Models;
using MudBlazor;
using System.Net.Http.Json;
using System.Text.Json;

namespace BloodThinnerTracker.Web.Services;

/// <summary>
/// Service implementation for medication management operations using HttpClient.
/// </summary>
public class MedicationService : IMedicationService
{
    private readonly HttpClient _httpClient;
    private readonly ISnackbar _snackbar;
    private readonly ILogger<MedicationService> _logger;
    private const string BaseUrl = "api/medications";

    public MedicationService(
        HttpClient httpClient,
        ISnackbar snackbar,
        ILogger<MedicationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _snackbar = snackbar ?? throw new ArgumentNullException(nameof(snackbar));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<List<Medication>> GetMedicationsAsync()
    {
        try
        {
            var medications = await _httpClient.GetFromJsonAsync<List<Medication>>(BaseUrl);
            return medications ?? new List<Medication>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve medications");
            _snackbar.Add("Failed to load medications. Please try again.", Severity.Error);
            return new List<Medication>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse medications response");
            _snackbar.Add("Error processing medication data.", Severity.Error);
            return new List<Medication>();
        }
    }

    /// <inheritdoc/>
    public async Task<Medication?> GetMedicationByIdAsync(string id)
    {
        try
        {
            var medication = await _httpClient.GetFromJsonAsync<Medication>($"{BaseUrl}/{id}");
            return medication;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Medication with ID {MedicationId} not found", id);
            _snackbar.Add("Medication not found.", Severity.Warning);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve medication {MedicationId}", id);
            _snackbar.Add("Failed to load medication details. Please try again.", Severity.Error);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse medication {MedicationId} response", id);
            _snackbar.Add("Error processing medication data.", Severity.Error);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<Medication?> CreateMedicationAsync(Medication medication)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, medication);

            if (response.IsSuccessStatusCode)
            {
                var createdMedication = await response.Content.ReadFromJsonAsync<Medication>();
                _snackbar.Add($"Medication '{medication.Name}' created successfully.", Severity.Success);
                _logger.LogInformation("Created medication {MedicationName}", medication.Name);
                return createdMedication;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _snackbar.Add("A medication with this name already exists.", Severity.Warning);
                return null;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create medication. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                _snackbar.Add("Failed to create medication. Please try again.", Severity.Error);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating medication {MedicationName}", medication.Name);
            _snackbar.Add("Network error creating medication. Please check your connection.", Severity.Error);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse create medication response");
            _snackbar.Add("Error processing server response.", Severity.Error);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateMedicationAsync(string id, Medication medication)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", medication);

            if (response.IsSuccessStatusCode)
            {
                _snackbar.Add($"Medication '{medication.Name}' updated successfully.", Severity.Success);
                _logger.LogInformation("Updated medication {MedicationId}", id);
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _snackbar.Add("Medication not found.", Severity.Warning);
                return false;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _snackbar.Add("A medication with this name already exists.", Severity.Warning);
                return false;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update medication {MedicationId}. Status: {StatusCode}, Error: {Error}",
                    id, response.StatusCode, errorContent);
                _snackbar.Add("Failed to update medication. Please try again.", Severity.Error);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating medication {MedicationId}", id);
            _snackbar.Add("Network error updating medication. Please check your connection.", Severity.Error);
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse update medication response");
            _snackbar.Add("Error processing server response.", Severity.Error);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateMedicationAsync(string id)
    {
        try
        {
            var medication = await GetMedicationByIdAsync(id);
            if (medication == null)
                return false;

            medication.IsActive = false;
            return await UpdateMedicationAsync(id, medication);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating medication {MedicationId}", id);
            _snackbar.Add("Failed to deactivate medication. Please try again.", Severity.Error);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteMedicationAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                _snackbar.Add("Medication deleted successfully.", Severity.Success);
                _logger.LogInformation("Deleted medication {MedicationId}", id);
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _snackbar.Add("Medication not found.", Severity.Warning);
                return false;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete medication {MedicationId}. Status: {StatusCode}, Error: {Error}",
                    id, response.StatusCode, errorContent);
                _snackbar.Add("Failed to delete medication. Please try again.", Severity.Error);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting medication {MedicationId}", id);
            _snackbar.Add("Network error deleting medication. Please check your connection.", Severity.Error);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CheckDuplicateAsync(string name, string? excludeId = null)
    {
        try
        {
            var medications = await GetMedicationsAsync();
            int? excludeIdInt = null;
            if (!string.IsNullOrEmpty(excludeId) && int.TryParse(excludeId, out var parsedId))
            {
                excludeIdInt = parsedId;
            }
            
            return medications.Any(m =>
                m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                (!excludeIdInt.HasValue || m.Id != excludeIdInt.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate medication {MedicationName}", name);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Medication>> GetMedicationsDueForRefillAsync()
    {
        try
        {
            var medications = await GetMedicationsAsync();

            // Note: Current Medication model doesn't have refill tracking fields
            // This is a placeholder for future enhancement
            // TODO: Add QuantityOnHand and RefillThresholdDays to Medication model
            return new List<Medication>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medications due for refill");
            return new List<Medication>();
        }
    }
}
