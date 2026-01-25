using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Extensions;

/// <summary>
/// Extension methods for registering mock/in-memory services for offline development and testing.
/// Keeps MauiProgram.cs concise by encapsulating mock service registration logic here.
/// </summary>
public static class MockServicesExtensions
{
    /// <summary>
    /// Registers all mock services for offline development and testing.
    /// This includes authentication, INR, medication, and sync services.
    /// </summary>
    public static IServiceCollection AddMockServices(this IServiceCollection services)
    {
        // Mock authentication and OAuth
        services.AddSingleton<Services.IAuthService>(sp =>
            new Services.MockAuthService(sp.GetRequiredService<ILogger<Services.MockAuthService>>()));
        services.AddSingleton<Services.IOAuthConfigService, Services.MockOAuthConfigService>();

        // Mock INR services
        services.AddSingleton<Services.IInrService, Services.MockInrService>();
        services.AddSingleton<Services.IInrRepository, Services.InMemoryInrRepository>();

        // Mock medication services
        services.AddSingleton<Services.IMedicationService, Services.MockMedicationService>();
        services.AddSingleton<Services.IMedicationRepository, Services.InMemoryMedicationRepository>();
        services.AddSingleton<Services.IMedicationLogRepository, Services.InMemoryMedicationLogRepository>();

        // Mock API client and sync services (prevents remote API calls)
        services.AddSingleton<Services.IApiClientFacade>(sp =>
            new Services.MockApiClientFacade(sp.GetService<ILogger<Services.MockApiClientFacade>>()));
        services.AddSingleton<Services.IForegroundSyncService>(sp =>
            new Services.MockForegroundSyncService(sp.GetService<ILogger<Services.MockForegroundSyncService>>()));

        return services;
    }
}
