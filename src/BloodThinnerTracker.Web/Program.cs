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
        options.CallbackPath = string.IsNullOrEmpty(adOptions.CallbackPath) ? "/signin-oidc" : adOptions.CallbackPath;
        
        // Hook into the OAuth callback to redirect to our Blazor page
        options.Events.OnTicketReceived = context =>
        {
            // Redirect to our Blazor callback page which will handle token storage
            context.Response.Redirect($"/oauth-complete?provider=microsoft");
            context.HandleResponse();
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
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

// Add MVC controllers for OAuth challenge endpoints (T003-001)
builder.Services.AddControllers();

// Register CustomAuthenticationStateProvider for JWT token management (T003-001)
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();

// Add HttpContextAccessor for OAuth callback handling (T003-001)
builder.Services.AddHttpContextAccessor();

// Add HttpClient for API calls with authentication (T018k)
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

// Authentication/Authorization middleware required for [Authorize] attributes (T018i)
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Map MVC controllers for OAuth challenge endpoints (T003-001)
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
