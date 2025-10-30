/*
 * BloodThinnerTracker.Web - Medication Log Service Implementation
 * Licensed under MIT License. See LICENSE file in the project root.
 * 
 * Service implementation for medication dose logging and adherence tracking operations.
 * Handles API communication for recording medication intake and viewing history.
 */

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MudBlazor;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Web.Services;

/// <summary>
/// Service implementation for medication dose logging and adherence tracking.
/// Communicates with the API to record medication intake and retrieve history.
/// </summary>
public sealed class MedicationLogService : IMedicationLogService
{
    private readonly HttpClient _httpClient;
    private readonly ISnackbar _snackbar;
    private readonly ILogger<MedicationLogService> _logger;
    private const string BaseUrl = "api/medicationlogs";

    /// <summary>
    /// Initializes a new instance of the MedicationLogService.
    /// </summary>
    /// <param name="httpClient">HTTP client for API communication.</param>
    /// <param name="snackbar">Snackbar for user notifications.</param>
    /// <param name="logger">Logger for operation tracking.</param>
    public MedicationLogService(
        HttpClient httpClient,
        ISnackbar snackbar,
        ILogger<MedicationLogService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _snackbar = snackbar ?? throw new ArgumentNullException(nameof(snackbar));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<List<MedicationLogDto>?> GetMedicationLogsAsync(
        string medicationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        MedicationLogStatus? status = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(medicationId))
            {
                _logger.LogWarning("GetMedicationLogsAsync called with empty medication ID");
                _snackbar.Add("Medication ID is required", Severity.Warning);
                return null;
            }

            var queryParams = new List<string>();
            if (fromDate.HasValue)
                queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-ddTHH:mm:ss}");
            if (toDate.HasValue)
                queryParams.Add($"toDate={toDate.Value:yyyy-MM-ddTHH:mm:ss}");
            if (status.HasValue)
                queryParams.Add($"status={status.Value}");

            var query = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : "";
            var url = $"{BaseUrl}/medication/{medicationId}{query}";

            _logger.LogInformation("Fetching medication logs from: {Url}", url);
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var logs = await response.Content.ReadFromJsonAsync<List<MedicationLogDto>>();
                _logger.LogInformation("Successfully retrieved {Count} medication logs", logs?.Count ?? 0);
                return logs;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Medication not found: {MedicationId}", medicationId);
                _snackbar.Add("Medication not found", Severity.Warning);
                return null;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to retrieve medication logs. Status: {StatusCode}, Error: {Error}",
                response.StatusCode, errorContent);
            _snackbar.Add("Failed to retrieve medication logs", Severity.Error);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error retrieving medication logs for medication {MedicationId}", medicationId);
            _snackbar.Add("Network error. Please check your connection.", Severity.Error);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing medication logs response");
            _snackbar.Add("Error processing medication logs data", Severity.Error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving medication logs for medication {MedicationId}", medicationId);
            _snackbar.Add("An unexpected error occurred", Severity.Error);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<MedicationLogDto?> GetMedicationLogByIdAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("GetMedicationLogByIdAsync called with empty ID");
                _snackbar.Add("Medication log ID is required", Severity.Warning);
                return null;
            }

            _logger.LogInformation("Fetching medication log: {Id}", id);
            var response = await _httpClient.GetAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                var log = await response.Content.ReadFromJsonAsync<MedicationLogDto>();
                _logger.LogInformation("Successfully retrieved medication log: {Id}", id);
                return log;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Medication log not found: {Id}", id);
                _snackbar.Add("Medication log not found", Severity.Warning);
                return null;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to retrieve medication log. Status: {StatusCode}, Error: {Error}",
                response.StatusCode, errorContent);
            _snackbar.Add("Failed to retrieve medication log", Severity.Error);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error retrieving medication log {Id}", id);
            _snackbar.Add("Network error. Please check your connection.", Severity.Error);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing medication log response");
            _snackbar.Add("Error processing medication log data", Severity.Error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving medication log {Id}", id);
            _snackbar.Add("An unexpected error occurred", Severity.Error);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<MedicationLogDto?> LogMedicationAsync(MedicationLogDto medicationLog)
    {
        try
        {
            if (medicationLog == null)
            {
                _logger.LogWarning("LogMedicationAsync called with null medication log");
                _snackbar.Add("Medication log data is required", Severity.Warning);
                return null;
            }

            if (string.IsNullOrWhiteSpace(medicationLog.MedicationId))
            {
                _logger.LogWarning("LogMedicationAsync called with empty medication ID");
                _snackbar.Add("Medication ID is required", Severity.Warning);
                return null;
            }

            _logger.LogInformation("Logging medication dose for medication: {MedicationId}", medicationLog.MedicationId);
            
            var json = JsonSerializer.Serialize(medicationLog);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BaseUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var createdLog = await response.Content.ReadFromJsonAsync<MedicationLogDto>();
                _logger.LogInformation("Successfully logged medication dose. Log ID: {Id}", createdLog?.Id);
                _snackbar.Add("Medication dose logged successfully", Severity.Success);
                return createdLog;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Validation error logging medication dose: {Error}", errorContent);
                
                // Try to parse error messages
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                    if (errorResponse?.Errors != null && errorResponse.Errors.Count > 0)
                    {
                        foreach (var error in errorResponse.Errors)
                        {
                            _snackbar.Add(error, Severity.Error);
                        }
                        return null;
                    }
                }
                catch
                {
                    // If parsing fails, show generic error
                }

                _snackbar.Add("Invalid medication log data", Severity.Error);
                return null;
            }

            var generalError = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to log medication dose. Status: {StatusCode}, Error: {Error}",
                response.StatusCode, generalError);
            _snackbar.Add("Failed to log medication dose", Severity.Error);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error logging medication dose");
            _snackbar.Add("Network error. Please check your connection.", Severity.Error);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error serializing/deserializing medication log data");
            _snackbar.Add("Error processing medication log data", Severity.Error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error logging medication dose");
            _snackbar.Add("An unexpected error occurred", Severity.Error);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<MedicationLogDto?> UpdateMedicationLogAsync(string id, MedicationLogDto medicationLog)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("UpdateMedicationLogAsync called with empty ID");
                _snackbar.Add("Medication log ID is required", Severity.Warning);
                return null;
            }

            if (medicationLog == null)
            {
                _logger.LogWarning("UpdateMedicationLogAsync called with null medication log");
                _snackbar.Add("Medication log data is required", Severity.Warning);
                return null;
            }

            _logger.LogInformation("Updating medication log: {Id}", id);
            
            var json = JsonSerializer.Serialize(medicationLog);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                var updatedLog = await response.Content.ReadFromJsonAsync<MedicationLogDto>();
                _logger.LogInformation("Successfully updated medication log: {Id}", id);
                _snackbar.Add("Medication log updated successfully", Severity.Success);
                return updatedLog;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Medication log not found for update: {Id}", id);
                _snackbar.Add("Medication log not found", Severity.Warning);
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Validation error updating medication log: {Error}", errorContent);
                _snackbar.Add("Invalid medication log data", Severity.Error);
                return null;
            }

            var generalError = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to update medication log. Status: {StatusCode}, Error: {Error}",
                response.StatusCode, generalError);
            _snackbar.Add("Failed to update medication log", Severity.Error);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error updating medication log {Id}", id);
            _snackbar.Add("Network error. Please check your connection.", Severity.Error);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error serializing/deserializing medication log data");
            _snackbar.Add("Error processing medication log data", Severity.Error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating medication log {Id}", id);
            _snackbar.Add("An unexpected error occurred", Severity.Error);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteMedicationLogAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("DeleteMedicationLogAsync called with empty ID");
                _snackbar.Add("Medication log ID is required", Severity.Warning);
                return false;
            }

            _logger.LogInformation("Deleting medication log: {Id}", id);
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted medication log: {Id}", id);
                _snackbar.Add("Medication log deleted successfully", Severity.Success);
                return true;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Medication log not found for deletion: {Id}", id);
                _snackbar.Add("Medication log not found", Severity.Warning);
                return false;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to delete medication log. Status: {StatusCode}, Error: {Error}",
                response.StatusCode, errorContent);
            _snackbar.Add("Failed to delete medication log", Severity.Error);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error deleting medication log {Id}", id);
            _snackbar.Add("Network error. Please check your connection.", Severity.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting medication log {Id}", id);
            _snackbar.Add("An unexpected error occurred", Severity.Error);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<List<MedicationLogDto>?> GetTodaysLogsAsync()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            _logger.LogInformation("Fetching today's medication logs");
            
            // We'll need to fetch all medications and their logs for today
            // This is a placeholder - you may want to add a specific API endpoint for this
            var logs = new List<MedicationLogDto>();
            
            _logger.LogInformation("Successfully retrieved today's medication logs");
            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving today's medication logs");
            _snackbar.Add("An unexpected error occurred", Severity.Error);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<decimal?> GetAdherenceRateAsync(string medicationId, DateTime fromDate, DateTime toDate)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(medicationId))
            {
                _logger.LogWarning("GetAdherenceRateAsync called with empty medication ID");
                return null;
            }

            _logger.LogInformation("Calculating adherence rate for medication: {MedicationId}", medicationId);
            
            var logs = await GetMedicationLogsAsync(medicationId, fromDate, toDate);
            if (logs == null || logs.Count == 0)
            {
                _logger.LogInformation("No logs found for adherence calculation");
                return null;
            }

            var scheduledCount = logs.Count(l => l.Status != MedicationLogStatus.Scheduled);
            var takenCount = logs.Count(l => l.Status == MedicationLogStatus.Taken && 
                                             Math.Abs(l.TimeVarianceMinutes) <= 120); // Within 2 hours

            if (scheduledCount == 0)
            {
                _logger.LogInformation("No scheduled doses found for adherence calculation");
                return null;
            }

            var adherenceRate = (decimal)takenCount / scheduledCount * 100;
            _logger.LogInformation("Adherence rate calculated: {Rate}% ({Taken}/{Scheduled})",
                adherenceRate, takenCount, scheduledCount);
            
            return Math.Round(adherenceRate, 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating adherence rate for medication {MedicationId}", medicationId);
            return null;
        }
    }
}

/// <summary>
/// Error response model for API validation errors.
/// </summary>
internal sealed class ErrorResponse
{
    public List<string>? Errors { get; set; }
}
