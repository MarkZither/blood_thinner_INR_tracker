using BloodThinnerTracker.Api.Services;
using BloodThinnerTracker.Api.Services.Authentication;
using BloodThinnerTracker.Api.Hubs;
using BloodThinnerTracker.Shared.Models.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

// ⚠️ MEDICAL APPLICATION DISCLAIMER ⚠️
// This application handles medical data and must comply with healthcare regulations.
// Ensure proper security measures are in place before deployment.

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (logging, health checks, etc.)
// Note: Temporarily commented out until ServiceDefaults.AddServiceDefaults is properly implemented
// builder.AddServiceDefaults();

// Configure medical database with encryption and compliance features
builder.Services.AddMedicalDatabase(builder.Configuration, builder.Environment);

// Configure authentication and security for medical application
builder.Services.ConfigureMedicalAuthentication(builder.Configuration);

// Add HTTP context accessor for audit logging
builder.Services.AddHttpContextAccessor();

// Add API services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Blood Thinner Tracker API")
            .WithTheme(ScalarTheme.Mars)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithPreferredScheme("Bearer")
            .WithApiKeyAuthentication(x => x.Token = "your-api-key");
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Security middleware
app.UseHttpsRedirection();
app.UseCors("MedicalAppPolicy");

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
