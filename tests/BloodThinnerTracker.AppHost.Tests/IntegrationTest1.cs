using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BloodThinnerTracker.AppHost.Tests;

/// <summary>
/// Fixture for managing AppHost lifecycle across tests.
/// Creates a single AppHost instance that is shared across all tests in the class.
/// </summary>
public sealed class AppHostFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    public DistributedApplication App => _app ?? throw new InvalidOperationException("AppHost not initialized. Call InitializeAsync first.");

    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BloodThinnerTracker_AppHost>(cancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();

            // Only accept dangerous self-signed certificates when running in CI
            // (many CI runners set the `CI` env var to "true"). This keeps local
            // developer runs strict while allowing CI to succeed with test certs.
            var isCi = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
            if (isCi)
            {
                clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
            }
        });

        _app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await _app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}

public class AppHostIntegrationTests : IClassFixture<AppHostFixture>
{
    private readonly AppHostFixture _fixture;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    public AppHostIntegrationTests(AppHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AppHost_StartsSuccessfully()
    {
        // Arrange & Act - AppHost already started by fixture
        var app = _fixture.App;

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public async Task ApiResource_IsHealthy()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var app = _fixture.App;

        // Act
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Assert - if we reach here without timeout, API is healthy
        Assert.True(true);
    }

    [Fact]
    public async Task WebResource_IsHealthy()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var app = _fixture.App;

        // Act
        await app.ResourceNotifications.WaitForResourceHealthyAsync("web", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Assert - if we reach here without timeout, Web is healthy
        Assert.True(true);
    }

    [Fact]
    public async Task ApiHealthEndpoint_ReturnsOk()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var app = _fixture.App;

        // Act
        using var httpClient = app.CreateHttpClient("api");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        using var response = await httpClient.GetAsync("/health", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WebHealthEndpoint_ReturnsOk()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var app = _fixture.App;

        // Act
        using var httpClient = app.CreateHttpClient("web");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("web", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        using var response = await httpClient.GetAsync("/health", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
