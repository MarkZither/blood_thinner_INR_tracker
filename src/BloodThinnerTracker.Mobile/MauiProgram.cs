using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace BloodThinnerTracker.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { });

        // Register services (DI) - minimal bootstrap
        builder.Services.AddSingleton<App>();
        // Register MainPage for DI resolution in App
        builder.Services.AddSingleton<MainPage>();

        // Register Feature services (Phase 2 implementations)
        builder.Services.AddSingleton<Services.IInrService, Services.MockInrService>();
        builder.Services.AddSingleton<Services.EncryptionService>();
        builder.Services.AddSingleton<Services.ISecureStorageService, Services.SecureStorageService>();
        // HttpClient for API-backed services (ApiInrService and AuthService will use this)
        builder.Services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new System.Uri("https://api.example.invalid/");
        });
        builder.Services.AddSingleton(sp => sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient("ApiClient"));
        builder.Services.AddSingleton<Services.ApiInrService>();
        builder.Services.AddSingleton<Services.AuthService>(sp =>
        {
            var secure = sp.GetRequiredService<Services.ISecureStorageService>();
            var client = sp.GetRequiredService<System.Net.Http.HttpClient>();
            return new Services.AuthService(secure, client);
        });

        return builder.Build();
    }
}
