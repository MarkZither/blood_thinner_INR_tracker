using Microsoft.Extensions.Logging;
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

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        // Register services (DI) - minimal bootstrap
        builder.Services.AddSingleton<App>();
        // Register MainPage for DI resolution in App
        builder.Services.AddSingleton<MainPage>();

        // Register views and viewmodels for navigation
        builder.Services.AddTransient<Views.SplashView>();
        builder.Services.AddTransient<ViewModels.SplashViewModel>();
        builder.Services.AddTransient<Views.LoginView>();
        builder.Services.AddTransient<ViewModels.LoginViewModel>();

        // Register Feature services (Phase 2 implementations)
        // Use MockInrService in DEBUG, ApiInrService in Release (config-driven later)
    #if DEBUG
        builder.Services.AddSingleton<Services.IInrService, Services.MockInrService>();
    #else
        builder.Services.AddSingleton<Services.IInrService, Services.ApiInrService>();
    #endif
        builder.Services.AddSingleton<Services.EncryptionService>();
        builder.Services.AddSingleton<Services.ISecureStorageService, Services.SecureStorageService>();
        // HttpClient for API-backed services (ApiInrService and AuthService will use this)
        builder.Services.AddSingleton<System.Net.Http.HttpClient>(sp => new System.Net.Http.HttpClient
        {
            BaseAddress = new System.Uri("https://api.example.invalid/")
        });
        builder.Services.AddSingleton<Services.ApiInrService>();
        // Register AuthService as IAuthService so viewmodels can depend on the abstraction
        builder.Services.AddSingleton<Services.IAuthService>(sp =>
        {
            var secure = sp.GetRequiredService<Services.ISecureStorageService>();
            var client = sp.GetRequiredService<System.Net.Http.HttpClient>();
            return new Services.AuthService(secure, client);
        });

        return builder.Build();
    }
}
