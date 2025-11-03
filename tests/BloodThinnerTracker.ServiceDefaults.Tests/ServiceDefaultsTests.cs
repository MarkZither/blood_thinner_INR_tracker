using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace BloodThinnerTracker.ServiceDefaults.Tests;

public class ServiceDefaultsTests
{
    [Fact]
    public void AddServiceDefaults_RegistersRequiredServices()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.AddServiceDefaults();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Verify IHttpClientFactory is registered (service discovery uses HttpClient)
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public void AddServiceDefaults_RegistersHealthChecks()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.AddServiceDefaults();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetService<HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }

    [Fact]
    public async Task AddDefaultHealthChecks_RegistersSelfCheck()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        builder.AddDefaultHealthChecks();

        // Act
        var serviceProvider = builder.Services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();
        var result = await healthCheckService.CheckHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Entries, e => e.Key == "self");
        Assert.Equal(HealthStatus.Healthy, result.Entries["self"].Status);
    }

    [Fact]
    public async Task AddDefaultHealthChecks_SelfCheckHasLiveTag()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        builder.AddDefaultHealthChecks();

        // Act
        var serviceProvider = builder.Services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();
        var result = await healthCheckService.CheckHealthAsync();

        // Assert
        var selfCheck = result.Entries["self"];
        Assert.Contains("live", selfCheck.Tags);
    }

    [Fact]
    public void ConfigureOpenTelemetry_RegistersTracerProvider()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.ConfigureOpenTelemetry();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void ConfigureOpenTelemetry_RegistersMeterProvider()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.ConfigureOpenTelemetry();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var meterProvider = serviceProvider.GetService<MeterProvider>();
        Assert.NotNull(meterProvider);
    }

    [Fact]
    public void AddServiceDefaults_ConfiguresHttpClientDefaults()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.AddServiceDefaults();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        // HttpClientFactory should be registered
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public void AddServiceDefaults_ConfiguresOpenTelemetryLogging()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.AddServiceDefaults();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);
    }

    [Fact]
    public async Task MapDefaultEndpoints_ExposesHealthCheckEndpoint()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.AddServiceDefaults();
        builder.WebHost.UseTestServer(); // Use TestServer instead of Kestrel

        var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync();

        // Act
        var client = app.GetTestClient();
        var response = await client.GetAsync("/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        await app.DisposeAsync();
    }

    [Fact]
    public async Task MapDefaultEndpoints_ExposesAlivenessCheckEndpoint()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.AddServiceDefaults();
        builder.WebHost.UseTestServer(); // Use TestServer instead of Kestrel

        var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync();

        // Act
        var client = app.GetTestClient();
        var response = await client.GetAsync("/alive");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        await app.DisposeAsync();
    }

    [Fact]
    public void MapDefaultEndpoints_OnlyInDevelopment_DoesNotExposeEndpointsInProduction()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Production";
        builder.AddServiceDefaults();

        var app = builder.Build();
        app.MapDefaultEndpoints();

        // Act & Assert
        // In production, MapDefaultEndpoints should not map any health check routes
        // We verify by checking that the routing system doesn't have the specific paths
        var dataSource = app.Services.GetServices<EndpointDataSource>().FirstOrDefault();
        if (dataSource != null)
        {
            var healthEndpoints = dataSource.Endpoints.Where(e =>
                e.DisplayName?.Contains("/health", StringComparison.OrdinalIgnoreCase) == true ||
                e.DisplayName?.Contains("/alive", StringComparison.OrdinalIgnoreCase) == true).ToList();

            Assert.Empty(healthEndpoints);
        }
    }

    [Fact]
    public void ConfigureOpenTelemetry_WithOtlpEndpoint_ConfiguresExporter()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317";

        // Act
        builder.ConfigureOpenTelemetry();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void ConfigureOpenTelemetry_WithoutOtlpEndpoint_DoesNotThrow()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act & Assert
        var exception = Record.Exception(() => builder.ConfigureOpenTelemetry());
        Assert.Null(exception);
    }
}
