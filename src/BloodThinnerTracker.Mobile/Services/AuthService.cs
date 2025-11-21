using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Services
{
    public class AuthExchangeRequest
    {
        public string Id_Token { get; set; } = string.Empty;
        public string Provider { get; set; } = "";
    }

    public class AuthExchangeResponse
    {
        public string Access_Token { get; set; } = string.Empty;
        public string Token_Type { get; set; } = "Bearer";
        public int Expires_In { get; set; }
        public DateTimeOffset Issued_At { get; set; }
    }

    public class AuthService
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly HttpClient _httpClient;

        private const string AccessTokenKey = "inr_access_token";

        public AuthService(ISecureStorageService secureStorage, HttpClient httpClient)
        {
            _secureStorage = secureStorage;
            _httpClient = httpClient;
        }

        // SignInAsync is a stub for the OAuth PKCE flow; real implementation will use platform browser flows
        public Task<string> SignInAsync()
        {
            // In MVP we return a placeholder id_token for integration tests / mocked flows
            return Task.FromResult("mock-id-token");
        }

        public async Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure")
        {
            var req = new { id_token = idToken, provider };
            try
            {
                var resp = await _httpClient.PostAsJsonAsync("auth/exchange", req);
                if (!resp.IsSuccessStatusCode)
                    return false;

                var body = await resp.Content.ReadFromJsonAsync<AuthExchangeResponse>();
                if (body == null) return false;

                await _secureStorage.SetAsync(AccessTokenKey, body.Access_Token);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            return await _secureStorage.GetAsync(AccessTokenKey);
        }

        public async Task SignOutAsync()
        {
            await _secureStorage.RemoveAsync(AccessTokenKey);
        }
    }
}
