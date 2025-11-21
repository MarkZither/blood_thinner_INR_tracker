using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.ViewModels;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Mobile.Services
{
    public class ApiInrService : IInrService
    {
        private readonly HttpClient _httpClient;

        public ApiInrService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 5)
        {
            var uri = $"api/inr/recent?count={count}";
            try
            {
                var result = await _httpClient.GetFromJsonAsync<IEnumerable<INRTestResponse>>(uri);
                if (result == null) return Array.Empty<InrListItemVm>();

                return result.Select(r => new InrListItemVm
                {
                    PublicId = r.PublicId,
                    TestDate = r.TestDate,
                    InrValue = r.INRValue,
                    Notes = r.Notes,
                    ReviewedByProvider = r.ReviewedByProvider
                });
            }
            catch (Exception)
            {
                return Array.Empty<InrListItemVm>();
            }
        }
    }
}
