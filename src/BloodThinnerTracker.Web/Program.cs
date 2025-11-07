using BloodThinnerTracker.Web.Components;
using BloodThinnerTracker.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.Google;
using BloodThinnerTracker.Shared.Models.Authentication;
using MudBlazor.Services;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault integration for production secrets
if (builder.Environment.IsProduction())
{
    var keyVaultUri = builder.Configuration["ConnectionStrings:KeyVaultUri"];
    if (!string.IsNullOrEmpty(keyVaultUri))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUri),
            new Azure.Identity.DefaultAzureCredential());
    }
}

// Add service defaults (OpenTelemetry, health checks, service discovery, resilience)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();


// Bind authentication config to POCOs using options pattern
builder.Services.Configure<BloodThinnerTracker.Shared.Models.Authentication.AzureAdConfig>(
    builder.Configuration.GetSection("Authentication:AzureAd"));
builder.Services.Configure<BloodThinnerTracker.Shared.Models.Authentication.GoogleOptions>(
    builder.Configuration.GetSection("Authentication:Google"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = MicrosoftAccountDefaults.AuthenticationScheme;
})
    .AddCookie()
    .AddMicrosoftAccount(options =>
    {
        var adOptions = builder.Configuration.GetSection("Authentication:AzureAd").Get<BloodThinnerTracker.Shared.Models.Authentication.AzureAdConfig>() ?? new BloodThinnerTracker.Shared.Models.Authentication.AzureAdConfig();
        options.ClientId = adOptions.ClientId;
        options.ClientSecret = adOptions.ClientSecret;
        options.SaveTokens = true;

        // Add OpenID Connect scopes (keep defaults and add what we need)
        // Don't clear - Microsoft Account provider needs its default scopes
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.CallbackPath = string.IsNullOrEmpty(adOptions.CallbackPath) ? "/signin-oidc" : adOptions.CallbackPath;

        // Handle OAuth failures (network errors, API timeouts, etc.)
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/login?error=oauth_failed&message=" + Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed"));
            context.HandleResponse();
            return Task.CompletedTask;
        };

        // After successful authentication, redirect to our Blazor callback page
        options.Events.OnTicketReceived = context =>
        {
            // Get the returnUrl from authentication properties
            var returnUrl = context.Properties?.RedirectUri ?? "/dashboard";

            // Redirect to our Blazor callback page which will exchange tokens
            // The authentication cookies have been set by this point
            var redirectUrl = $"/oauth-complete?provider=microsoft&returnUrl={Uri.EscapeDataString(returnUrl)}";

            context.ReturnUri = redirectUrl;
            context.Properties!.RedirectUri = redirectUrl;

            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        var googleOptions = builder.Configuration.GetSection("Authentication:Google").Get<BloodThinnerTracker.Shared.Models.Authentication.GoogleOptions>() ?? new BloodThinnerTracker.Shared.Models.Authentication.GoogleOptions();
        options.ClientId = googleOptions.ClientId;
        options.ClientSecret = googleOptions.ClientSecret;
        options.SaveTokens = true;
        options.CallbackPath = string.IsNullOrEmpty(googleOptions.CallbackPath) ? "/signin-google" : googleOptions.CallbackPath;

        // Handle OAuth failures (network errors, API timeouts, etc.)
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/login?error=oauth_failed&message=" + Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed"));
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

// Add memory cache for server-side authentication state storage (T003-001)
builder.Services.AddMemoryCache();

// Add session support for memory cache key isolation (T003-001)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add MVC controllers for OAuth challenge endpoints (T003-001)
builder.Services.AddControllers();

// Register CustomAuthenticationStateProvider for JWT token management (T003-001)
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();

// Add HttpContextAccessor for OAuth callback handling (T003-001)
builder.Services.AddHttpContextAccessor();

// Register services for UI (T003-004, T003-005, T003-005b)
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IINRService, BloodThinnerTracker.Web.Services.INRService>();
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IMedicationService, BloodThinnerTracker.Web.Services.MedicationService>();
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IMedicationLogService, BloodThinnerTracker.Web.Services.MedicationLogService>();
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IMedicationPatternService, BloodThinnerTracker.Web.Services.MedicationPatternService>();
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IMedicationScheduleService, BloodThinnerTracker.Web.Services.MedicationScheduleService>();

// Add HttpClient for API calls with authentication (T003-001)
builder.Services.AddTransient<AuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();

    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7234")
    };

    return httpClient;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// TODO: Re-enable createScopeForErrors parameter when .NET 10 final is released
// This overload is documented but not yet available in RC 2
app.UseStatusCodePagesWithReExecute("/not-found");

app.UseHttpsRedirection();

// Session middleware (must come before authentication for session-based cache keys)
app.UseSession();

// Authentication/Authorization middleware required for [Authorize] attributes (T018i)
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Map MVC controllers for OAuth challenge endpoints (T003-001)
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map Aspire health check endpoints (/health, /alive)
app.MapDefaultEndpoints();

app.Run();
