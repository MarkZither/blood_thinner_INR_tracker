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

            // API returns a list of INRTestResponse DTOs. Map each to the client INRTest domain model.
            var dtos = await _httpClient.GetFromJsonAsync<List<INRTestResponse>>(url);
            if (dtos == null) return new List<INRTest>();
            return dtos.Select(MapResponseToEntity).ToList();
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
            var url = $"{BaseUrl}/{testId}";
            // Use GetAsync so we can log response body on 404 for diagnostics
            using var resp = await _httpClient.GetAsync(url);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("INR test {TestId} not found for URL {Url}. Response: {Body}", testId, url, body);
                return null;
            }

            resp.EnsureSuccessStatusCode();
            var dto = await resp.Content.ReadFromJsonAsync<INRTestResponse>();
            if (dto == null) return null;
            return MapResponseToEntity(dto);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "INR test {TestId} not found (HttpRequestException)", testId);
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
            // Use PublicId for API route
            var publicId = test.PublicId; // should be set by caller
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{publicId}", test);
            response.EnsureSuccessStatusCode();

            // FIX: API returns INRTestResponse with string IDs (GUIDs), convert to INRTest with int IDs
            var responseDto = await response.Content.ReadFromJsonAsync<INRTestResponse>()
                ?? throw new InvalidOperationException("Failed to deserialize updated test");

            return MapResponseToEntity(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating INR test {TestPublicId}", test.PublicId);
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
            // Map typed PublicId from API response
            PublicId = response.PublicId,
            // Note: Client's internal int IDs are not used for API calls; preserve 0
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
