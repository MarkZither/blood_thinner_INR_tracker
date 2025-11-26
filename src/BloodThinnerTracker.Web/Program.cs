using BloodThinnerTracker.ServiceDefaults.Services;
using Microsoft.Extensions.Logging;
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
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using Microsoft.AspNetCore.HttpOverrides;


// Configure Serilog for file logging before builder creation
// Load minimal configuration early so logging templates and paths can be configured
var preConfig = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

    // Enable Serilog SelfLog conditionally to help diagnose sink/configuration errors
    var enableSelfLog = preConfig.GetValue<bool?>("Diagnostics:Serilog:EnableSelfLog") ?? false;
    if (enableSelfLog)
    {
        var selfLogPath = preConfig["Diagnostics:Serilog:SelfLogPath"];
        if (!string.IsNullOrEmpty(selfLogPath))
        {
            try
            {
                var resolved = Path.IsPathRooted(selfLogPath) ? selfLogPath : Path.Combine(AppContext.BaseDirectory, selfLogPath);
                var sw = File.CreateText(resolved);
                sw.AutoFlush = true;
                Serilog.Debugging.SelfLog.Enable(TextWriter.Synchronized(sw));
            }
            catch (Exception ex)
            {
                Serilog.Debugging.SelfLog.WriteLine($"Failed to enable SelfLog file '{selfLogPath}': {ex}");
                Serilog.Debugging.SelfLog.Enable(Console.Error);
            }
        }
        else
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);
        }
    }

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(preConfig)
        .Enrich.FromLogContext()
        .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog
builder.Host.UseSerilog();

// Run as a Windows Service when deployed on Windows hosts
if (OperatingSystem.IsWindows())
{
    builder.Host.UseWindowsService();
}

// Configure Kestrel certificate for HTTPS endpoints (optional, cross-platform)
// Use standard ASP.NET Core configuration via appsettings.json or environment variables:
// - Kestrel:Certificates:Default:Thumbprint (Windows only - cert in LocalMachine\My)
// - Kestrel:Certificates:Default:Path (PFX file path or PEM certificate path)
// - Kestrel:Certificates:Default:Password (PFX password, not needed for PEM)
// - Kestrel:Certificates:Default:KeyPath (PEM private key path, optional if using PEM)
//
// Supported formats:
//   1. Windows Certificate Store (thumbprint) - Windows only
//   2. PFX/PKCS12 (.pfx/.p12) - Cross-platform, requires password
//   3. PEM (.crt + .key) - Cross-platform, no password needed, RECOMMENDED for Linux/containers
//
// IMPORTANT: Do NOT hardcode ports or ListenAnyIP here!
// Use standard Urls configuration: appsettings.json "Urls": "http://localhost:5235;https://localhost:7235"
// Or environment variable: ASPNETCORE_URLS=http://localhost:5235;https://localhost:7235
// Or launchSettings.json for development
var certThumb = builder.Configuration["Kestrel:Certificates:Default:Thumbprint"];
var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"];
var certPassword = builder.Configuration["Kestrel:Certificates:Default:Password"];
var keyPath = builder.Configuration["Kestrel:Certificates:Default:KeyPath"];

if (!string.IsNullOrEmpty(certThumb) || !string.IsNullOrEmpty(certPath))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            try
            {
                // Windows-specific: Load certificate from Windows Certificate Store by thumbprint
                if (!string.IsNullOrEmpty(certThumb) && OperatingSystem.IsWindows())
                {
                    using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var certs = store.Certificates.Find(X509FindType.FindByThumbprint, certThumb, false);
                    if (certs.Count > 0)
                    {
                        httpsOptions.ServerCertificate = certs[0];
                    }
                    store.Close();
                }
                // Cross-platform: Load certificate from PEM files (.crt + .key) - RECOMMENDED
                else if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(keyPath) &&
                         File.Exists(certPath) && File.Exists(keyPath))
                {
                    var certPem = File.ReadAllText(certPath);
                    var keyPem = File.ReadAllText(keyPath);
                    httpsOptions.ServerCertificate = X509Certificate2.CreateFromPem(certPem, keyPem);
                }
                // Cross-platform: Load certificate from PFX file (.pfx/.p12)
                else if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
                {
                    httpsOptions.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail startup - allows app to start without HTTPS
                // Operators should check certificate configuration if HTTPS is required
                Console.WriteLine($"Warning: Failed to load certificate: {ex.Message}");
            }
        });
    });
}

// Configure Azure Key Vault integration for production secrets (shared extension)
// Configure Azure Key Vault integration for production secrets (ServiceDefaults extension)
builder.Host.ConfigureAppConfiguration((hostingContext, configBuilder) =>
{
    using var loggerFactory = LoggerFactory.Create(logging => logging.AddSerilog(Log.Logger));
    var logger = loggerFactory.CreateLogger("KeyVault");
    BloodThinnerTracker.ServiceDefaults.Services.KeyVaultConfigurationExtensions.UseKeyVaultIfConfigured(
        configBuilder,
        hostingContext.HostingEnvironment,
        builder.Configuration,
        logger);
});

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
    options.DefaultChallengeScheme = "AzureAD"; // Use our custom OIDC scheme
})
    .AddCookie(options =>
    {
        // Configure cookie lifetime to match our token lifetime
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true; // Extend cookie on activity
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // Redirect to login on unauthorized access
        options.LoginPath = "/login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/access-denied";
    })
    .AddOpenIdConnect("AzureAD", options =>
    {
        var adOptions = builder.Configuration.GetSection("Authentication:AzureAd").Get<BloodThinnerTracker.Shared.Models.Authentication.AzureAdConfig>() ?? new BloodThinnerTracker.Shared.Models.Authentication.AzureAdConfig();

        // Azure AD v2.0 endpoints - Use organizational tenant for consistent oid claim
        // Using TenantId instead of 'common' ensures we get real oid (not fake MSA oid)
        var tenantId = adOptions.TenantId ?? "common";
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.ClientId = adOptions.ClientId;
        options.ClientSecret = adOptions.ClientSecret;
        options.CallbackPath = string.IsNullOrEmpty(adOptions.CallbackPath) ? "/signin-oidc" : adOptions.CallbackPath;
        options.ResponseType = "code";  // Authorization code flow - id_token comes from token endpoint
        options.ResponseMode = "query";  // Standard query string response

        options.SaveTokens = true;  // Save tokens to authentication properties
        options.GetClaimsFromUserInfoEndpoint = true;  // Get additional claims from userinfo endpoint

        // Token validation for organizational tenant
        // ValidateIssuer = true when using specific tenant (not 'common')
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = tenantId != "common",  // Validate issuer for specific tenant
            ValidateAudience = true,
            ValidAudience = adOptions.ClientId,
            ValidateLifetime = true
        };

        // Request OIDC scopes
        options.Scope.Clear();  // Clear defaults
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Map Azure AD claims to principal - DON'T clear defaults, just add oid mapping
        // The middleware will extract claims from id_token and put them in the principal
        options.ClaimActions.MapJsonKey("oid", "oid");  // Map oid claim directly
        options.ClaimActions.MapJsonKey("tid", "tid");  // Tenant ID

        // Handle OAuth failures
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/login?error=oauth_failed&message=" + Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed"));
            context.HandleResponse();
            return Task.CompletedTask;
        };

        // Redirect to our Blazor callback after OIDC completes
        options.Events.OnTicketReceived = context =>
        {
            var returnUrl = context.Properties?.RedirectUri ?? "/dashboard";
            var redirectUrl = $"/oauth-complete?provider=microsoft&returnUrl={Uri.EscapeDataString(returnUrl)}";

            context.ReturnUri = redirectUrl;
            context.Properties!.RedirectUri = redirectUrl;

            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var tokens = context.Properties.GetTokens();
            foreach (var token in tokens)
            {
                logger.LogInformation("OIDC token available: {Name}, length: {Length}", token.Name, token.Value?.Length ?? 0);
            }

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

// Add HttpClient factory for creating bare HTTP clients (for token refresh without auth header)
builder.Services.AddHttpClient();

// Register services for UI (T003-004, T003-005, T003-005b)
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IINRService, BloodThinnerTracker.Web.Services.INRService>();
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IMedicationService, BloodThinnerTracker.Web.Services.MedicationService>();
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IMedicationLogService, BloodThinnerTracker.Web.Services.MedicationLogService>();
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IMedicationPatternService, BloodThinnerTracker.Web.Services.MedicationPatternService>();
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IMedicationScheduleService, BloodThinnerTracker.Web.Services.MedicationScheduleService>();
builder.Services.AddScoped<BloodThinnerTracker.Web.Services.IPageStateService, BloodThinnerTracker.Web.Services.PageStateService>();

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


// Configure cookie policy to work correctly when behind a TLS-terminating
// reverse proxy (Traefik, nginx, etc.). Note: Cookie authentication options above
// configure the authentication cookie settings, while this section configures
// the general cookie policy for the application.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Forwarded headers middleware must run early, before authentication and any URL generation
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};

// Read optional trusted proxy configuration from ForwardedHeaders section.
// In Development we continue to accept forwarded headers from local proxies for ease of testing.
// In Production you MUST populate ForwardedHeaders:KnownProxies or ForwardedHeaders:KnownNetworks
// with your reverse proxy / load balancer IPs or CIDRs. See docs/deployment/forwarded-headers.md
// for guidance on how to obtain those values (Traefik, Kubernetes, cloud load-balancers).
// For local development with a trusted proxy (Traefik on same host) accept forwarded headers
// from the host proxy. Clear both KnownProxies and KnownNetworks so the middleware will
// apply X-Forwarded-* headers coming from Docker/Traefik on the host network.
// NOTE: This is only for development convenience. In production, populate specific
// KnownProxies / KnownIPNetworks with your proxy IPs or CIDRs (see docs/deployment/forwarded-headers.md).
forwardedOptions.KnownProxies.Clear();
forwardedOptions.KnownIPNetworks.Clear();
// Some proxy setups omit symmetric header pairs; relax symmetry check for local testing.
forwardedOptions.RequireHeaderSymmetry = false;
app.UseForwardedHeaders(forwardedOptions);

// Diagnostic middleware to log the scheme/host seen by the app (temporary, helpful while testing with proxy)
app.Use(async (ctx, next) =>
{
    var loggerFactory = ctx.RequestServices.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("ForwardedDebug");
    logger.LogInformation("ForwardedDebug: Scheme={Scheme} Host={Host} Path={Path} XFP={XFP}",
        ctx.Request.Scheme, ctx.Request.Host, ctx.Request.Path, ctx.Request.Headers["X-Forwarded-Proto"].ToString());
    await next();
});

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

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Web application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
