using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Configuration;

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

        // Load configuration from appsettings.json embedded resource
        using var stream = typeof(App).Assembly.GetManifestResourceStream("BloodThinnerTracker.Mobile.appsettings.json");
        if (stream != null)
        {
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddInMemoryCollection(config.AsEnumerable());
        }

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        // Register services (DI) - minimal bootstrap
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<AppShell>();
        // Register MainPage for DI resolution in App
        builder.Services.AddSingleton<MainPage>();

        // Register views and viewmodels for navigation
        builder.Services.AddTransient<Views.LoginView>();
        builder.Services.AddTransient<ViewModels.LoginViewModel>();
        builder.Services.AddTransient<Views.InrListView>();
        // InrListViewModel created lazily in InrListView.xaml.cs to avoid premature service initialization
        builder.Services.AddTransient<Views.AboutView>();

        // Register Feature services - use configuration flag instead of #if DEBUG
        // Features.UseMockServices: true = mock, false = real API
        // Note: JSON boolean true becomes string "True" (capitalized) - use case-insensitive comparison
        var useMockServices = builder.Configuration["Features:UseMockServices"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        builder.Services.AddSingleton<Services.IInrService>(sp =>
        {
            return useMockServices
                ? (Services.IInrService)new Services.MockInrService()
                : new Services.ApiInrService(sp.GetRequiredService<System.Net.Http.HttpClient>());
        });

        builder.Services.AddSingleton<Services.EncryptionService>();
        builder.Services.AddSingleton<Services.ISecureStorageService, Services.SecureStorageService>();

        // HttpClient for API-backed services
        var oauthConfigUrl = builder.Configuration["OAuth:OAuthConfigUrl"] ?? "https://api.example.invalid/";
        builder.Services.AddSingleton<System.Net.Http.HttpClient>(sp => new System.Net.Http.HttpClient
        {
            BaseAddress = new System.Uri(oauthConfigUrl)
        });
        builder.Services.AddSingleton<Services.ApiInrService>();

        // OAuth configuration service - use configuration flag for mock/real
        builder.Services.AddSingleton<Services.IOAuthConfigService>(sp =>
        {
            return useMockServices
                ? (Services.IOAuthConfigService)new Services.MockOAuthConfigService()
                : new Services.OAuthConfigService(
                    sp.GetRequiredService<System.Net.Http.HttpClient>(),
                    sp.GetRequiredService<ILogger<Services.OAuthConfigService>>());
        });

        // Register AuthService - use configuration flag for mock/real
        builder.Services.AddSingleton<Services.IAuthService>(sp =>
        {
            return useMockServices
                ? (Services.IAuthService)new Services.MockAuthService(
                    sp.GetRequiredService<ILogger<Services.MockAuthService>>())
                : new Services.AuthService(
                    sp.GetRequiredService<Services.ISecureStorageService>(),
                    sp.GetRequiredService<Services.IOAuthConfigService>(),
                    sp.GetRequiredService<System.Net.Http.HttpClient>(),
                    sp.GetRequiredService<ILogger<Services.AuthService>>());
        });

        return builder.Build();
    }
}
