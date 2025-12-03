using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.ViewModels;
using BloodThinnerTracker.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BloodThinnerTracker.Mobile.Services
{
    public class ApiInrService : IInrService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiInrService> _logger;

        public ApiInrService(HttpClient httpClient, ILogger<ApiInrService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 5)
        {
            // Use canonical API endpoint with paging/filter parameters
            // Map `count` -> `take` and include `skip=0` for latest records
            var uri = $"api/v1/inr/tests?take={count}&skip=0";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await SafeReadResponseBodyAsync(response);
                    _logger.LogWarning("INR API returned {StatusCode} for {Uri}. Body: {Body}", (int)response.StatusCode, uri, Truncate(body, 2000));

                    // If the API indicates the user is unauthorized, surface an authentication-specific
                    // exception so higher layers (UI) can navigate back to login and show feedback.
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new ApiAuthenticationException(HttpStatusCode.Unauthorized, body);
                    }

                    return Enumerable.Empty<InrListItemVm>();
                }

                var result = await response.Content.ReadFromJsonAsync<IEnumerable<INRTestResponse>>();
                if (result == null)
                {
                    return Enumerable.Empty<InrListItemVm>();
                }

                return result.Select(r => new InrListItemVm
                {
                    PublicId = r.PublicId,
                    TestDate = r.TestDate,
                    InrValue = r.INRValue,
                    Notes = r.Notes,
                    ReviewedByProvider = r.ReviewedByProvider
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error while calling INR API for recent tests (count={Count})", count);
                return Enumerable.Empty<InrListItemVm>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching recent INR tests (count={Count})", count);
                throw;
            }
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length <= maxLength) return value;
            return value.Substring(0, maxLength) + "...";
        }

        private static async Task<string> SafeReadResponseBodyAsync(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
