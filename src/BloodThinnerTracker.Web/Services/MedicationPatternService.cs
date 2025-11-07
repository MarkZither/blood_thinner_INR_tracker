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
        Guid medicationPublicId,
        CreateDosagePatternRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creating dosage pattern for medication {MedicationPublicId}, pattern length {Length}",
                medicationPublicId, request.PatternSequence.Count);

            var response = await _httpClient.PostAsJsonAsync(
                $"api/medications/{medicationPublicId}/patterns",
                request);

            if (response.IsSuccessStatusCode)
            {
                var pattern = await response.Content.ReadFromJsonAsync<DosagePatternResponse>();

                _logger.LogInformation(
                    "Successfully created pattern {PatternId} for medication {MedicationPublicId}",
                    pattern?.Id, medicationPublicId);

                return pattern;
            }

            // Log error details
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Failed to create pattern for medication {MedicationPublicId}. Status: {StatusCode}, Error: {Error}",
                medicationPublicId, response.StatusCode, errorContent);

            response.EnsureSuccessStatusCode(); // Throw exception with details
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating dosage pattern for medication {MedicationPublicId}",
                medicationPublicId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DosagePatternResponse?> GetActivePatternAsync(Guid medicationPublicId)
    {
        try
        {
            _logger.LogDebug(
                "Fetching active pattern for medication {MedicationPublicId}",
                medicationPublicId);

            var response = await _httpClient.GetAsync(
                $"api/medications/{medicationPublicId}/patterns/active");

            if (response.IsSuccessStatusCode)
            {
                var pattern = await response.Content.ReadFromJsonAsync<DosagePatternResponse>();

                _logger.LogDebug(
                    "Retrieved active pattern {PatternId} for medication {MedicationPublicId}",
                    pattern?.Id, medicationPublicId);

                return pattern;
            }

            // 404 is expected when no active pattern exists
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug(
                    "No active pattern found for medication {MedicationPublicId}",
                    medicationPublicId);
                return null;
            }

            // Log unexpected errors
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Unexpected error fetching active pattern for medication {MedicationPublicId}. Status: {StatusCode}, Error: {Error}",
                medicationPublicId, response.StatusCode, errorContent);

            response.EnsureSuccessStatusCode();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching active pattern for medication {MedicationPublicId}",
                medicationPublicId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PatternHistoryResponse?> GetPatternHistoryAsync(
        Guid medicationPublicId,
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
                "Fetching pattern history for medication {MedicationPublicId}, activeOnly={ActiveOnly}, limit={Limit}, offset={Offset}",
                medicationPublicId, activeOnly, limit, offset);

            var response = await _httpClient.GetAsync(
                $"api/medications/{medicationPublicId}/patterns?activeOnly={activeOnly}&limit={limit}&offset={offset}");

            if (response.IsSuccessStatusCode)
            {
                var history = await response.Content.ReadFromJsonAsync<PatternHistoryResponse>();

                _logger.LogDebug(
                    "Retrieved {Count} patterns for medication {MedicationPublicId}",
                    history?.Patterns.Count ?? 0, medicationPublicId);

                return history;
            }

            // Log error details
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Failed to fetch pattern history for medication {MedicationPublicId}. Status: {StatusCode}, Error: {Error}",
                medicationPublicId, response.StatusCode, errorContent);

            response.EnsureSuccessStatusCode();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching pattern history for medication {MedicationPublicId}",
                medicationPublicId);
            throw;
        }
    }
}
