using System.Net.Http.Json;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Web.Services;

/// <summary>
/// HTTP-based implementation of INR service that calls the API
/// </summary>
public class INRService : IINRService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<INRService> _logger;
    private const string BaseUrl = "/api/v1/inr/tests";

    public INRService(HttpClient httpClient, ILogger<INRService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<INRTest>> GetTestsAsync(DateTime? startDate = null, DateTime? endDate = null, bool? inRange = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            if (inRange.HasValue)
                queryParams.Add($"inRange={inRange.Value}");

            var url = BaseUrl;
            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.GetFromJsonAsync<INRTestListResponse>(url);
            return response?.Tests ?? new List<INRTest>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching INR tests");
            throw;
        }
    }

    public async Task<INRTest?> GetTestByIdAsync(Guid testId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<INRTest>($"{BaseUrl}/{testId}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("INR test {TestId} not found", testId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching INR test {TestId}", testId);
            throw;
        }
    }

    public async Task<INRTest> CreateTestAsync(INRTest test)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, test);
            response.EnsureSuccessStatusCode();
            
            // FIX: API returns INRTestResponse with string IDs (GUIDs), convert to INRTest with int IDs
            var responseDto = await response.Content.ReadFromJsonAsync<INRTestResponse>()
                ?? throw new InvalidOperationException("Failed to deserialize created test");
            
            return MapResponseToEntity(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating INR test");
            throw;
        }
    }

    public async Task<INRTest> UpdateTestAsync(INRTest test)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{test.Id}", test);
            response.EnsureSuccessStatusCode();
            
            // FIX: API returns INRTestResponse with string IDs (GUIDs), convert to INRTest with int IDs
            var responseDto = await response.Content.ReadFromJsonAsync<INRTestResponse>()
                ?? throw new InvalidOperationException("Failed to deserialize updated test");
            
            return MapResponseToEntity(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating INR test {TestId}", test.Id);
            throw;
        }
    }

    public async Task DeleteTestAsync(Guid testId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{testId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting INR test {TestId}", testId);
            throw;
        }
    }

    public async Task<INRTest?> GetLastTestAsync()
    {
        try
        {
            var tests = await GetTestsAsync();
            return tests.OrderByDescending(t => t.TestDate).FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching last INR test");
            throw;
        }
    }

    /// <summary>
    /// Maps INRTestResponse from API (string IDs) to INRTest domain model (int IDs).
    /// Note: Cannot convert GUID strings to int IDs, so they are set to 0.
    /// The UI should use PublicId (GUID) for operations, not int Id.
    /// </summary>
    private static INRTest MapResponseToEntity(INRTestResponse response)
    {
        return new INRTest
        {
            // Note: Cannot parse GUID string to int, so set to 0
            // The UI should use PublicId (GUID) for operations, not int Id
            Id = 0,
            UserId = 0,
            TestDate = response.TestDate,
            INRValue = response.INRValue,
            TargetINRMin = response.TargetINRMin,
            TargetINRMax = response.TargetINRMax,
            ProthrombinTime = response.ProthrombinTime,
            PartialThromboplastinTime = response.PartialThromboplastinTime,
            Laboratory = response.Laboratory,
            OrderedBy = response.OrderedBy,
            TestMethod = response.TestMethod,
            IsPointOfCare = response.IsPointOfCare,
            WasFasting = response.WasFasting,
            LastMedicationTime = response.LastMedicationTime,
            MedicationsTaken = response.MedicationsTaken,
            FoodsConsumed = response.FoodsConsumed,
            HealthConditions = response.HealthConditions,
            Status = response.Status,
            RecommendedActions = response.RecommendedActions,
            DosageChanges = response.DosageChanges,
            NextTestDate = response.NextTestDate,
            Notes = response.Notes,
            ReviewedByProvider = response.ReviewedByProvider,
            ReviewedBy = response.ReviewedBy,
            ReviewedAt = response.ReviewedAt,
            PatientNotified = response.PatientNotified,
            NotificationMethod = response.NotificationMethod,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt ?? DateTime.UtcNow
        };
    }

    // Response model matching API contract
    private class INRTestListResponse
    {
        public List<INRTest> Tests { get; set; } = new();
    }
}
