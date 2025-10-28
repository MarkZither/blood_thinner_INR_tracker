using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Shared.Services
{
    /// <summary>
    /// Minimal KeyVault accessor used by API/Web projects.
    /// This wrapper prefers configuration values (including values loaded from Azure Key Vault via AddAzureKeyVault)
    /// and falls back to returning null when a secret isn't present.
    /// </summary>
    public class KeyVaultService
    {
        private readonly IConfiguration _configuration;

        public KeyVaultService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Get a secret value by name. Looks up configuration first.
        /// </summary>
        public Task<string?> GetSecretAsync(string secretName)
        {
            // Try configuration keys (both flat and sectioned styles)
            var value = _configuration[secretName]
                ?? _configuration[$"Secrets:{secretName}"]
                ?? _configuration[$"ConnectionStrings:{secretName}"]
                ?? _configuration[$"Database:{secretName}"];

            return Task.FromResult(value);
        }
    }
}
