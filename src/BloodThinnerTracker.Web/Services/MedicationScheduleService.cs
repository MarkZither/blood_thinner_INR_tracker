namespace BloodThinnerTracker.Web.Services;

using BloodThinnerTracker.Shared.Models;
using System.Net.Http.Json;

/// <summary>
/// HTTP client service for retrieving medication dosage schedules from the API.
/// </summary>
public class MedicationScheduleService : IMedicationScheduleService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MedicationScheduleService> _logger;

    public MedicationScheduleService(
        HttpClient httpClient,
        ILogger<MedicationScheduleService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MedicationScheduleResponse?> GetScheduleAsync(
        Guid medicationPublicId,
        DateTime? startDate = null,
        int days = 14,
        bool includePatternChanges = true)
    {
        try
        {
            // Build query string
            var queryParams = new List<string>
            {
                $"days={days}",
                $"includePatternChanges={includePatternChanges.ToString().ToLower()}"
            };

            if (startDate.HasValue)
            {
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            }

            var queryString = string.Join("&", queryParams);
            var url = $"api/medications/{medicationPublicId}/schedule?{queryString}";

            _logger.LogInformation(
                "Fetching {Days}-day schedule for medication {MedicationId}",
                days, medicationPublicId);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to fetch schedule for medication {MedicationId}: {StatusCode}",
                    medicationPublicId, response.StatusCode);
                return null;
            }

            var schedule = await response.Content.ReadFromJsonAsync<MedicationScheduleResponse>();

            _logger.LogInformation(
                "Successfully fetched {Days}-day schedule for medication {MedicationId}, total dosage: {TotalDosage}",
                days, medicationPublicId, schedule?.Summary.TotalDosage ?? 0);

            return schedule;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP error fetching schedule for medication {MedicationId}",
                medicationPublicId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error fetching schedule for medication {MedicationId}",
                medicationPublicId);
            return null;
        }
    }
}
