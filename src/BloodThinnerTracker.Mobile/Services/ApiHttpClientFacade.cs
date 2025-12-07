using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiSdk.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Abstractions;
using ApiSdk;

namespace BloodThinnerTracker.Mobile.Services
{
    public class ApiHttpClientFacade : IApiClientFacade
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiHttpClientFacade>? _logger;
        private readonly ApiClient? _apiClient;

        public ApiHttpClientFacade(HttpClient httpClient, IAuthService authService, ILogger<ApiHttpClientFacade> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
            try
            {
                // Use the provided HttpClient so the adapter inherits its BaseAddress
                // and HTTP settings. Also set the adapter BaseUrl explicitly from the
                // HttpClient BaseAddress to ensure generated UrlTemplates resolve.
                var adapter = new HttpClientRequestAdapter(new KiotaTokenAuthenticationProvider(authService));
                if (_httpClient.BaseAddress != null)
                {
                    adapter.BaseUrl = _httpClient.BaseAddress.ToString();
                }
                _apiClient = new ApiClient(adapter);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create Kiota ApiClient request adapter");
            }
        }

        public async Task<List<MedicationResponse>?> GetMedicationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_apiClient == null)
                {
                    _logger?.LogWarning("ApiClient not initialized");
                    return null;
                }

                // Use generated Kiota client to get medications (no magic URIs)
                var result = await _apiClient.Api.V1.Medications.GetAsync(null, cancellationToken).ConfigureAwait(false);
                return result?.ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "GetMedicationsAsync failed");
                return null;
            }
        }

        public async Task<List<global::ApiSdk.Models.MedicationLogResponse>?> GetMedicationLogsAsync(Guid medicationPublicId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_apiClient == null)
                {
                    _logger?.LogWarning("ApiClient not initialized");
                    return null;
                }

                // Use the generated Kiota client to fetch medication logs for a specific medication
                // (there is no global list endpoint for all logs per user at present).
                var result = await _apiClient.Api.V1.MedicationLogs.Medication[medicationPublicId].GetAsync(null, cancellationToken).ConfigureAwait(false);
                return result?.ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "GetMedicationLogsAsync failed");
                return null;
            }
        }

        public async Task<List<global::ApiSdk.Models.INRTestResponse>?> GetRecentInrTestsAsync(int take = 50, CancellationToken cancellationToken = default)
        {
            try
            {
                var uri = $"api/v1/inr/tests?take={take}&skip=0";
                using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                using var resp = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await SafeReadAsync(resp).ConfigureAwait(false);
                    _logger?.LogWarning("GetRecentInrTestsAsync returned {Status} {Body}", (int)resp.StatusCode, body);
                    return null;
                }

                var result = await resp.Content.ReadFromJsonAsync<List<global::ApiSdk.Models.INRTestResponse>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "GetRecentInrTestsAsync failed");
                return null;
            }
        }

        private static async Task<string> SafeReadAsync(HttpResponseMessage resp)
        {
            try
            {
                return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
