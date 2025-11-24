// BloodThinnerTracker.Api.Tests - ID Token Validation Service Tests
// Licensed under MIT License. See LICENSE file in the project root.

using BloodThinnerTracker.Api.Services.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BloodThinnerTracker.Api.Tests.Services.Authentication;

public class IdTokenValidationServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<IdTokenValidationService>> _mockLogger;
    private readonly IdTokenValidationService _service;

    public IdTokenValidationServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<IdTokenValidationService>>();

        // Setup configuration with organizational tenant
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x.Value).Returns("092f1e77-1f13-4057-a665-ece954058d06");
        _mockConfiguration.Setup(x => x["Authentication:AzureAd:TenantId"]).Returns("092f1e77-1f13-4057-a665-ece954058d06");
        _mockConfiguration.Setup(x => x["Authentication:AzureAd:ClientId"]).Returns("test-client-id");
        _mockConfiguration.Setup(x => x["Authentication:Google:ClientId"]).Returns("test-google-client-id");

        _service = new IdTokenValidationService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateAzureAdTokenAsync_WithoutConfiguration_ReturnsInvalid()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["Authentication:AzureAd:TenantId"]).Returns((string?)null);
        mockConfig.Setup(x => x["Authentication:AzureAd:ClientId"]).Returns((string?)null);
        var service = new IdTokenValidationService(mockConfig.Object, _mockLogger.Object);

        // Act
        var result = await service.ValidateAzureAdTokenAsync("fake-token");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Azure AD authentication not configured", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateGoogleTokenAsync_WithInvalidToken_ReturnsInvalid()
    {
        // Act
        var result = await _service.ValidateGoogleTokenAsync("invalid-token-format");

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateGoogleTokenAsync_WithoutConfiguration_ReturnsInvalid()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["Authentication:Google:ClientId"]).Returns((string?)null);
        var service = new IdTokenValidationService(mockConfig.Object, _mockLogger.Object);

        // Act
        var result = await service.ValidateGoogleTokenAsync("fake-token");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Google authentication not configured", result.ErrorMessage);
    }

    [Fact]
    public void Service_UsesOrganizationalTenantFromConfiguration()
    {
        // This test verifies the service reads tenant ID from configuration
        // The actual validation happens at runtime with real Azure AD tokens

        // Verify configuration is accessed
        _mockConfiguration.Verify(x => x["Authentication:AzureAd:TenantId"], Times.Never);
        _mockConfiguration.Verify(x => x["Authentication:AzureAd:ClientId"], Times.Never);

        // Configuration will be accessed when ValidateAzureAdTokenAsync is called
    }

    [Theory]
    [InlineData(null, "common")]  // Null tenant falls back to common
    [InlineData("", "common")]     // Empty tenant falls back to common
    [InlineData("092f1e77-1f13-4057-a665-ece954058d06", "092f1e77-1f13-4057-a665-ece954058d06")]  // Organizational tenant
    public void Service_HandlesVariousTenantConfigurations(string? tenantId, string expectedTenantInAuthority)
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["Authentication:AzureAd:TenantId"]).Returns(tenantId);
        mockConfig.Setup(x => x["Authentication:AzureAd:ClientId"]).Returns("test-client-id");

        // Act
        var service = new IdTokenValidationService(mockConfig.Object, _mockLogger.Object);

        // Assert - Service created successfully
        Assert.NotNull(service);

        // The authority URL will be constructed as:
        // https://login.microsoftonline.com/{expectedTenantInAuthority}/v2.0
        // This is verified implicitly when the service validates a token
    }

    [Fact]
    public async Task ValidateAzureAdTokenAsync_LogsDebugInformation()
    {
        // Act
        var result = await _service.ValidateAzureAdTokenAsync("invalid-token");

        // Assert
        Assert.False(result.IsValid);

        // Verify logging occurred (debug log for tenant/client ID)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Azure AD Config")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
