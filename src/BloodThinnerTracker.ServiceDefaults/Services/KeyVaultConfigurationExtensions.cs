using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.ServiceDefaults.Services;

public static class KeyVaultConfigurationExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source if running in production and a KeyVaultUri is provided.
    /// Logs status using Microsoft.Extensions.Logging.
    /// </summary>
    /// <param name="configBuilder">The configuration builder.</param>
    /// <param name="environment">The host environment.</param>
    /// <param name="config">The configuration for fallback lookup.</param>
    /// <param name="logger">Optional ILogger for diagnostics.</param>
    public static void UseKeyVaultIfConfigured(
        this IConfigurationBuilder configBuilder,
        IHostEnvironment environment,
        IConfiguration config,
        ILogger? logger = null)
    {
        if (!environment.IsProduction())
            return;

        var keyVaultUri = Environment.GetEnvironmentVariable("KeyVaultUri") ?? config["ConnectionStrings:KeyVaultUri"];
        if (!string.IsNullOrEmpty(keyVaultUri))
        {
            try
            {
                configBuilder.AddAzureKeyVault(
                    new Uri(keyVaultUri),
                    new DefaultAzureCredential());
                logger?.LogInformation("[KeyVault] Azure Key Vault configuration loaded from {KeyVaultUri}", keyVaultUri);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[KeyVault] Failed to load Azure Key Vault: {Message}", ex.Message);
            }
        }
        else
        {
            logger?.LogWarning("[KeyVault] No KeyVaultUri provided. Skipping Azure Key Vault integration.");
        }
    }
}
