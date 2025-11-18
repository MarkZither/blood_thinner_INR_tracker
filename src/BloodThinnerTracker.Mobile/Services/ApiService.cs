using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// Service for communicating with the Blood Thinner Tracker API
/// </summary>
public interface IApiService
{
    Task<TResult?> GetAsync<TResult>(string endpoint);
    Task<TResult?> PostAsync<TData, TResult>(string endpoint, TData data);
    Task<TResult?> PutAsync<TData, TResult>(string endpoint, TData data);
    Task<bool> DeleteAsync(string endpoint);
    Task<bool> IsApiAvailableAsync();
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(IHttpClientFactory httpClientFactory, IAuthenticationService authService)
    {
        _httpClient = httpClientFactory.CreateClient("BloodThinnerApi");
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<TResult?> GetAsync<TResult>(string endpoint)
    {
        try
        {
            await SetAuthenticationHeaderAsync();
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResult>(content, _jsonOptions);
            }

            await HandleErrorResponse(response);
            return default;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API GET error: {ex.Message}");
            return default;
        }
    }

    public async Task<TResult?> PostAsync<TData, TResult>(string endpoint, TData data)
    {
        try
        {
            await SetAuthenticationHeaderAsync();
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResult>(responseContent, _jsonOptions);
            }

            await HandleErrorResponse(response);
            return default;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API POST error: {ex.Message}");
            return default;
        }
    }

    public async Task<TResult?> PutAsync<TData, TResult>(string endpoint, TData data)
    {
        try
        {
            await SetAuthenticationHeaderAsync();
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResult>(responseContent, _jsonOptions);
            }

            await HandleErrorResponse(response);
            return default;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API PUT error: {ex.Message}");
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            await SetAuthenticationHeaderAsync();
            var response = await _httpClient.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            await HandleErrorResponse(response);
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API DELETE error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task SetAuthenticationHeaderAsync()
    {
        var token = await _authService.GetAuthTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    private async Task HandleErrorResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine($"API Error {response.StatusCode}: {content}");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
        }
    }
}

/// <summary>
/// Medical data service for managing medications and INR tests
/// </summary>
public interface IMedicalDataService
{
    // Medication methods
    Task<List<Medication>?> GetMedicationsAsync();
    Task<Medication?> AddMedicationAsync(Medication medication);
    Task<Medication?> UpdateMedicationAsync(Medication medication);
    Task<bool> DeleteMedicationAsync(int medicationId);

    // Medication logs
    Task<List<MedicationLog>?> GetMedicationLogsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<MedicationLog?> LogMedicationAsync(MedicationLog log);
    Task<MedicationLog?> UpdateMedicationLogAsync(MedicationLog log);

    // INR tests
    Task<List<INRTest>?> GetINRTestsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<INRTest?> AddINRTestAsync(INRTest test);
    Task<INRTest?> UpdateINRTestAsync(INRTest test);
    Task<bool> DeleteINRTestAsync(Guid testPublicId);

    // Statistics
    Task<MedicationAdherenceStats?> GetMedicationAdherenceAsync(DateTime fromDate, DateTime toDate);
    Task<INRStats?> GetINRStatsAsync(DateTime fromDate, DateTime toDate);
}

public class MedicalDataService : IMedicalDataService
{
    private readonly IApiService _apiService;

    public MedicalDataService(IApiService apiService)
    {
        _apiService = apiService;
    }

    // Medication methods
    public async Task<List<Medication>?> GetMedicationsAsync()
    {
        return await _apiService.GetAsync<List<Medication>>("medications");
    }

    public async Task<Medication?> AddMedicationAsync(Medication medication)
    {
        return await _apiService.PostAsync<Medication, Medication>("medications", medication);
    }

    public async Task<Medication?> UpdateMedicationAsync(Medication medication)
    {
        return await _apiService.PutAsync<Medication, Medication>($"medications/{medication.Id}", medication);
    }

    public async Task<bool> DeleteMedicationAsync(int medicationId)
    {
        return await _apiService.DeleteAsync($"medications/{medicationId}");
    }

    // Medication logs
    public async Task<List<MedicationLog>?> GetMedicationLogsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var endpoint = "medicationlogs";
        if (fromDate.HasValue || toDate.HasValue)
        {
            var query = new List<string>();
            if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            endpoint += "?" + string.Join("&", query);
        }

        return await _apiService.GetAsync<List<MedicationLog>>(endpoint);
    }

    public async Task<MedicationLog?> LogMedicationAsync(MedicationLog log)
    {
        return await _apiService.PostAsync<MedicationLog, MedicationLog>("medicationlogs", log);
    }

    public async Task<MedicationLog?> UpdateMedicationLogAsync(MedicationLog log)
    {
        return await _apiService.PutAsync<MedicationLog, MedicationLog>($"medicationlogs/{log.Id}", log);
    }

    // INR tests
    public async Task<List<INRTest>?> GetINRTestsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var endpoint = "inrtests";
        if (fromDate.HasValue || toDate.HasValue)
        {
            var query = new List<string>();
            if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            endpoint += "?" + string.Join("&", query);
        }

        return await _apiService.GetAsync<List<INRTest>>(endpoint);
    }

    public async Task<INRTest?> AddINRTestAsync(INRTest test)
    {
        return await _apiService.PostAsync<INRTest, INRTest>("inrtests", test);
    }

    public async Task<INRTest?> UpdateINRTestAsync(INRTest test)
    {
        // Use PublicId (GUID) for API routes
        var publicId = test.PublicId != Guid.Empty ? test.PublicId : Guid.Empty;
        return await _apiService.PutAsync<INRTest, INRTest>($"inrtests/{publicId}", test);
    }

    public async Task<bool> DeleteINRTestAsync(Guid testPublicId)
    {
        return await _apiService.DeleteAsync($"inrtests/{testPublicId}");
    }

    // Statistics
    public async Task<MedicationAdherenceStats?> GetMedicationAdherenceAsync(DateTime fromDate, DateTime toDate)
    {
        return await _apiService.GetAsync<MedicationAdherenceStats>(
            $"medicationlogs/adherence?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}");
    }

    public async Task<INRStats?> GetINRStatsAsync(DateTime fromDate, DateTime toDate)
    {
        return await _apiService.GetAsync<INRStats>(
            $"inrtests/stats?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}");
    }
}

// Statistics models
public class MedicationAdherenceStats
{
    public int TotalDoses { get; set; }
    public int TakenDoses { get; set; }
    public int MissedDoses { get; set; }
    public int DelayedDoses { get; set; }
    public double AdherencePercentage { get; set; }
}

public class INRStats
{
    public int TotalTests { get; set; }
    public int TestsInRange { get; set; }
    public double TimeInRangePercentage { get; set; }
    public double AverageINR { get; set; }
    public double LatestINR { get; set; }
    public DateTime? LastTestDate { get; set; }
}
