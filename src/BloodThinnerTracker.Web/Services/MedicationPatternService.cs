namespace BloodThinnerTracker.Web.Services;

using BloodThinnerTracker.Shared.Models;
using System.Net.Http.Json;

/// <summary>
/// Service implementation for managing medication dosage patterns via API.
/// </summary>
/// <remarks>
/// This service uses HttpClient to communicate with the BloodThinnerTracker.Api backend.
/// All requests include JWT authentication tokens automatically via configured HttpClient.
/// 
/// Error handling:
/// - 401 Unauthorized: User not authenticated (redirects to login)
/// - 403 Forbidden: User doesn't own the medication
/// - 404 Not Found: Medication or pattern doesn't exist
/// - 400 Bad Request: Validation errors (returned in response)
/// </remarks>
public class MedicationPatternService : IMedicationPatternService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MedicationPatternService> _logger;

    public MedicationPatternService(
        HttpClient httpClient,
        ILogger<MedicationPatternService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DosagePatternResponse?> CreatePatternAsync(
        int medicationId,
        CreateDosagePatternRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creating dosage pattern for medication {MedicationId}, pattern length {Length}",
                medicationId, request.PatternSequence.Count);

            var response = await _httpClient.PostAsJsonAsync(
                $"api/medications/{medicationId}/patterns",
                request);

            if (response.IsSuccessStatusCode)
            {
                var pattern = await response.Content.ReadFromJsonAsync<DosagePatternResponse>();
                
                _logger.LogInformation(
                    "Successfully created pattern {PatternId} for medication {MedicationId}",
                    pattern?.Id, medicationId);

                return pattern;
            }

            // Log error details
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Failed to create pattern for medication {MedicationId}. Status: {StatusCode}, Error: {Error}",
                medicationId, response.StatusCode, errorContent);

            response.EnsureSuccessStatusCode(); // Throw exception with details
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating dosage pattern for medication {MedicationId}",
                medicationId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DosagePatternResponse?> GetActivePatternAsync(int medicationId)
    {
        try
        {
            _logger.LogDebug(
                "Fetching active pattern for medication {MedicationId}",
                medicationId);

            var response = await _httpClient.GetAsync(
                $"api/medications/{medicationId}/patterns/active");

            if (response.IsSuccessStatusCode)
            {
                var pattern = await response.Content.ReadFromJsonAsync<DosagePatternResponse>();
                
                _logger.LogDebug(
                    "Retrieved active pattern {PatternId} for medication {MedicationId}",
                    pattern?.Id, medicationId);

                return pattern;
            }

            // 404 is expected when no active pattern exists
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug(
                    "No active pattern found for medication {MedicationId}",
                    medicationId);
                return null;
            }

            // Log unexpected errors
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Unexpected error fetching active pattern for medication {MedicationId}. Status: {StatusCode}, Error: {Error}",
                medicationId, response.StatusCode, errorContent);

            response.EnsureSuccessStatusCode();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching active pattern for medication {MedicationId}",
                medicationId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PatternHistoryResponse?> GetPatternHistoryAsync(
        int medicationId,
        bool activeOnly = false,
        int limit = 10,
        int offset = 0)
    {
        try
        {
            // Validate parameters
            limit = Math.Clamp(limit, 1, 100);
            offset = Math.Max(offset, 0);

            _logger.LogDebug(
                "Fetching pattern history for medication {MedicationId}, activeOnly={ActiveOnly}, limit={Limit}, offset={Offset}",
                medicationId, activeOnly, limit, offset);

            var response = await _httpClient.GetAsync(
                $"api/medications/{medicationId}/patterns?activeOnly={activeOnly}&limit={limit}&offset={offset}");

            if (response.IsSuccessStatusCode)
            {
                var history = await response.Content.ReadFromJsonAsync<PatternHistoryResponse>();
                
                _logger.LogDebug(
                    "Retrieved {Count} patterns for medication {MedicationId}",
                    history?.Patterns.Count ?? 0, medicationId);

                return history;
            }

            // Log error details
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Failed to fetch pattern history for medication {MedicationId}. Status: {StatusCode}, Error: {Error}",
                medicationId, response.StatusCode, errorContent);

            response.EnsureSuccessStatusCode();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching pattern history for medication {MedicationId}",
                medicationId);
            throw;
        }
    }
}
