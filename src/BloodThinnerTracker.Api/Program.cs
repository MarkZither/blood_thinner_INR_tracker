using BloodThinnerTracker.Api.Services;
using BloodThinnerTracker.Api.Services.Authentication;
using BloodThinnerTracker.Api.Hubs;
using BloodThinnerTracker.Shared.Models.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using System.Text;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

// ⚠️ MEDICAL APPLICATION DISCLAIMER ⚠️
// This application handles medical data and must comply with healthcare regulations.
// Ensure proper security measures are in place before deployment.

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

// Configure medical database with encryption and compliance features
builder.Services.AddMedicalDatabase(builder.Configuration, builder.Environment);

// Configure authentication and security for medical application
builder.Services.ConfigureMedicalAuthentication(builder.Configuration);

// Add HTTP context accessor for audit logging
builder.Services.AddHttpContextAccessor();

// Add API services
builder.Services.AddControllers();

// Add OpenAPI generation with custom documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Blood Thinner Tracker API";
        document.Info.Version = "v1";
        document.Info.Description = """
            # 🔐 Authentication Required

            This API uses **OAuth 2.0 authentication** with JWT bearer tokens.

            ## Quick Start: Get Your Token (30 seconds)

            1. **Get a JWT Token**: Open [/oauth-test.html](/oauth-test.html) in a new tab
            2. **Login**: Click "Login with Google" or "Login with Azure AD"
            3. **Copy Token**: Click the "Copy Token" button after successful login
            4. **Use Token in Scalar**:
               - For each API request, click the "Headers" tab
               - Add header: `Authorization`
               - Value: `Bearer {paste-your-token-here}`
            5. **Send Request**: Your request will now include authentication

            ✅ **You're authenticated!** The API will recognize you as the logged-in user.

            ## 📚 Documentation

            - **OAuth Testing Guide**: [OAUTH_TESTING_GUIDE.md](https://github.com/MarkZither/blood_thinner_INR_tracker/blob/main/docs/OAUTH_TESTING_GUIDE.md)
            - **Authentication Guide**: [AUTHENTICATION_TESTING_GUIDE.md](https://github.com/MarkZither/blood_thinner_INR_tracker/blob/main/docs/AUTHENTICATION_TESTING_GUIDE.md)

            ## 🏥 Medical Application Disclaimer

            ⚠️ **This application handles medical data and is for informational purposes only.**

            - Always consult healthcare professionals for medical decisions
            - This system is for medication tracking purposes only
            - Not a substitute for professional medical advice
            - Complies with healthcare data protection measures

            ## 🔒 Security Features

            - Medical data encryption (AES-256)
            - Audit logging for compliance
            - User data isolation
            - OWASP security guidelines
            - Healthcare data protection
            """;

        return Task.CompletedTask;
    });
});

// Add distributed cache for OAuth state parameter storage
builder.Services.AddDistributedMemoryCache(); // Use Redis in production: builder.Services.AddStackExchangeRedisCache(...)

// Add SignalR for real-time medical notifications
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

// Add medical notification service
builder.Services.AddScoped<BloodThinnerTracker.Api.Hubs.IMedicalNotificationService, BloodThinnerTracker.Api.Hubs.MedicalNotificationService>();

// Add background service for medical reminders
builder.Services.AddHostedService<MedicalReminderService>();

// Add CORS for web client with SignalR support
builder.Services.AddCors(options =>
{
    options.AddPolicy("MedicalAppPolicy", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7001", // Web app
                "https://bloodtracker.com",
                "https://dev.bloodtracker.com",
                "https://staging.bloodtracker.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required for SignalR
    });
});

// Configure JSON options for medical data
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = false;
});

var app = builder.Build();

// Initialize database on startup
await app.Services.EnsureDatabaseAsync();

    app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Blood Thinner Tracker API - JWT Authentication Required")
        .WithTheme(ScalarTheme.Mars)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithModels(false); // Hide schemas section for cleaner UI

    // Note: To test authenticated endpoints in Scalar:
    // 1. Visit /oauth-test.html to get a JWT token
    // 2. In Scalar UI, for each request:
    //    - Click "Headers" tab
    //    - Add header: Authorization
    //    - Value: Bearer {paste-your-token-here}
    // 3. Send request - it will include your auth token
    //
    // See docs/OAUTH_TESTING_GUIDE.md for detailed instructions
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Security middleware
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("MedicalAppPolicy");

// Static files middleware (serves wwwroot/oauth-test.html and other static content)
app.UseStaticFiles();

// Medical application routes
app.UseRouting();

// Authentication and authorization for medical data security
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR hub for medical notifications
app.MapHub<BloodThinnerTracker.Api.Hubs.MedicalNotificationHub>("/hubs/medical-notifications");

// Health check endpoints for medical application monitoring
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Medical disclaimer endpoint
app.MapGet("/disclaimer", () => new
{
    Disclaimer = "⚠️ MEDICAL DISCLAIMER: This software is for informational purposes only and should not replace professional medical advice. Always consult with your healthcare provider regarding your medication schedule.",
    ComplianceNote = "This application implements healthcare data protection measures including encryption, audit logging, and user data isolation.",
    Version = "1.0.0",
    LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd")
})
.WithName("GetMedicalDisclaimer")
.WithSummary("Medical application disclaimer and compliance information");

// Default route redirects to Scalar API documentation
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
    .ExcludeFromDescription();

// Legacy info endpoint
app.MapGet("/info", () => new
{
    Application = "Blood Thinner Medication & INR Tracker API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Status = "Operational",
    MedicalDisclaimer = "⚠️ This is a medical application. Always consult healthcare providers for medical advice.",
    Endpoints = new
    {
        ApiDocumentation = "/scalar/v1",
        Health = "/health",
        Disclaimer = "/disclaimer",
        OpenAPI = app.Environment.IsDevelopment() ? "/openapi/v1.json" : "Available in development only"
    },
    SecurityFeatures = new[]
    {
        "Medical data encryption",
        "Audit logging",
        "User data isolation",
        "Healthcare compliance measures"
    },
    ComplianceStandards = new[]
    {
        "OWASP Security Guidelines",
        "Healthcare Data Protection",
        "Medical Data Retention Policies"
    }
})
.WithName("GetApplicationInfo")
.WithSummary("Blood Thinner Tracker API information with medical compliance details");

app.Run();
