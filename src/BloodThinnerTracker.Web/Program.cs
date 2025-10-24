using BloodThinnerTracker.Web.Components;
using BloodThinnerTracker.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add authentication and authorization services (T018c, T018i)
// AddAuthentication is required for [Authorize] attributes in Blazor Server
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "BlazorScheme";
    options.DefaultChallengeScheme = "BlazorScheme";
})
.AddScheme<AuthenticationSchemeOptions, BlazorAuthenticationHandler>("BlazorScheme", options => { });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>(sp => 
    (CustomAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
