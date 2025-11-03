//-----------------------------------------------------------------------
// <copyright file="MedicationServiceErrorHandlingTests.cs" company="Blood Thinner Tracker">
//     Copyright (c) Blood Thinner Tracker. All rights reserved.
//     MEDICAL DISCLAIMER: This software is for informational purposes only.
//     Always consult with qualified healthcare professionals for medical decisions.
// </copyright>
//-----------------------------------------------------------------------

using System.Net;
using System.Text;
using System.Text.Json;
using BloodThinnerTracker.Shared.Models;
using BloodThinnerTracker.Web.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MudBlazor;

namespace BloodThinnerTracker.Web.Tests.Services;

/// <summary>
/// Tests for MedicationService error handling, verifying that specific
/// validation errors from the API are properly displayed to users.
/// </summary>
public sealed class MedicationServiceErrorHandlingTests
{
    private readonly Mock<ILogger<MedicationService>> _mockLogger;
    private readonly Mock<ISnackbar> _mockSnackbar;
    private readonly List<string> _snackbarMessages;

    public MedicationServiceErrorHandlingTests()
    {
        _mockLogger = new Mock<ILogger<MedicationService>>();
        _mockSnackbar = new Mock<ISnackbar>();
        _snackbarMessages = new List<string>();

        // Capture snackbar messages for verification
        _mockSnackbar
            .Setup(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
            .Callback<string, Severity, Action<SnackbarOptions>, string>((message, severity, configure, key) =>
            {
                _snackbarMessages.Add(message);
            })
            .Returns((Snackbar)null!);
    }

    [Fact]
    public async Task CreateMedicationAsync_WithWarfarinWithoutINRMonitoring_ShowsSpecificErrorMessage()
    {
        // Arrange
        var medication = new Medication
        {
            Name = "Test Warfarin",
            Type = MedicationType.VitKAntagonist,
            Dosage = 5.0m,
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = false
        };

        var validationErrors = new Dictionary<string, string[]>
        {
            ["validation"] = new[]
            {
                "Warfarin (Vitamin K Antagonist) requires INR monitoring for safety"
            }
        };

        var problemDetails = new
        {
            errors = validationErrors,
            detail = "Validation failed"
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(problemDetails));

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CreateMedicationAsync(medication);

        // Assert
        Assert.Null(result);
        Assert.Contains(_snackbarMessages,
            msg => msg.Contains("Warfarin") && msg.Contains("INR monitoring") && msg.Contains("safety"));
    }

    [Fact]
    public async Task CreateMedicationAsync_WithWarfarinWithoutINRTargetRange_ShowsSpecificErrorMessage()
    {
        // Arrange
        var medication = new Medication
        {
            Name = "Test Warfarin",
            Type = MedicationType.VitKAntagonist,
            Dosage = 5.0m,
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true
            // Missing INRTargetMin and INRTargetMax
        };

        var validationErrors = new Dictionary<string, string[]>
        {
            ["validation"] = new[]
            {
                "Warfarin requires INR target range (typically 2.0-3.0)"
            }
        };

        var problemDetails = new
        {
            errors = validationErrors,
            detail = "Validation failed"
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(problemDetails));

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CreateMedicationAsync(medication);

        // Assert
        Assert.Null(result);
        Assert.Contains(_snackbarMessages,
            msg => msg.Contains("Warfarin") && msg.Contains("INR target range"));
    }

    [Fact]
    public async Task CreateMedicationAsync_WithWarfarinAbove20mg_ShowsSpecificErrorMessage()
    {
        // Arrange
        var medication = new Medication
        {
            Name = "Test Warfarin",
            Type = MedicationType.VitKAntagonist,
            Dosage = 25.0m, // Above 20mg limit
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true,
            INRTargetMin = 2.0m,
            INRTargetMax = 3.0m
        };

        var validationErrors = new Dictionary<string, string[]>
        {
            ["validation"] = new[]
            {
                "Warfarin dosage above 20mg requires special attention"
            }
        };

        var problemDetails = new
        {
            errors = validationErrors,
            detail = "Validation failed"
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(problemDetails));

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CreateMedicationAsync(medication);

        // Assert
        Assert.Null(result);
        Assert.Contains(_snackbarMessages,
            msg => msg.Contains("Warfarin") && msg.Contains("20mg"));
    }

    [Fact]
    public async Task CreateMedicationAsync_WithMultipleValidationErrors_ShowsAllErrors()
    {
        // Arrange
        var medication = new Medication
        {
            Name = "Test Warfarin",
            Type = MedicationType.VitKAntagonist,
            Dosage = 25.0m,
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = false
        };

        var validationErrors = new Dictionary<string, string[]>
        {
            ["validation"] = new[]
            {
                "Warfarin (Vitamin K Antagonist) requires INR monitoring for safety",
                "Warfarin requires INR target range (typically 2.0-3.0)",
                "Warfarin dosage above 20mg requires special attention"
            }
        };

        var problemDetails = new
        {
            errors = validationErrors,
            detail = "Multiple validation errors"
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(problemDetails));

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CreateMedicationAsync(medication);

        // Assert
        Assert.Null(result);
        Assert.Equal(3, _snackbarMessages.Count);
        Assert.Contains(_snackbarMessages, msg => msg.Contains("INR monitoring"));
        Assert.Contains(_snackbarMessages, msg => msg.Contains("INR target range"));
        Assert.Contains(_snackbarMessages, msg => msg.Contains("20mg"));
    }

    [Fact]
    public async Task CreateMedicationAsync_WithDetailButNoErrors_ShowsDetailMessage()
    {
        // Arrange
        var medication = new Medication
        {
            Name = "Test Medication",
            Type = MedicationType.Antiplatelet,
            Dosage = 81.0m,
            Frequency = MedicationFrequency.OnceDaily
        };

        var problemDetails = new
        {
            detail = "Custom validation error message"
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(problemDetails));

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CreateMedicationAsync(medication);

        // Assert
        Assert.Null(result);
        Assert.Contains(_snackbarMessages, msg => msg.Contains("Custom validation error message"));
    }

    [Fact]
    public async Task CreateMedicationAsync_WithMalformedJsonResponse_ShowsGenericMessage()
    {
        // Arrange
        var medication = new Medication
        {
            Name = "Test Medication",
            Type = MedicationType.Antiplatelet,
            Dosage = 81.0m,
            Frequency = MedicationFrequency.OnceDaily
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.BadRequest,
            "This is not valid JSON {{{");

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CreateMedicationAsync(medication);

        // Assert
        Assert.Null(result);
        // When JSON parsing fails, it falls back to generic validation message
        Assert.Contains(_snackbarMessages, msg => msg.Contains("Medication validation failed"));
    }

    [Fact]
    public async Task UpdateMedicationAsync_WithValidationErrors_ShowsSpecificErrorMessages()
    {
        // Arrange
        var medicationId = "1";
        var medication = new Medication
        {
            Id = 1,
            Name = "Updated Warfarin",
            Type = MedicationType.VitKAntagonist,
            Dosage = 5.0m,
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = false
        };

        var validationErrors = new Dictionary<string, string[]>
        {
            ["validation"] = new[]
            {
                "Warfarin (Vitamin K Antagonist) requires INR monitoring for safety"
            }
        };

        var problemDetails = new
        {
            errors = validationErrors,
            detail = "Validation failed"
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(problemDetails));

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.UpdateMedicationAsync(medicationId, medication);

        // Assert
        Assert.False(result);
        Assert.Contains(_snackbarMessages,
            msg => msg.Contains("Warfarin") && msg.Contains("INR monitoring"));
    }

    [Fact]
    public async Task UpdateMedicationAsync_WithMalformedJsonResponse_ShowsGenericMessage()
    {
        // Arrange
        var medicationId = "1";
        var medication = new Medication
        {
            Id = 1,
            Name = "Updated Medication",
            Type = MedicationType.Antiplatelet,
            Dosage = 81.0m,
            Frequency = MedicationFrequency.OnceDaily
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.BadRequest,
            "Invalid JSON");

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.UpdateMedicationAsync(medicationId, medication);

        // Assert
        Assert.False(result);
        Assert.Contains(_snackbarMessages, msg => msg.Contains("Medication validation failed"));
    }

    [Fact]
    public async Task CreateMedicationAsync_WithInternalServerError_ShowsGenericMessage()
    {
        // Arrange
        var medication = new Medication
        {
            Name = "Test Medication",
            Type = MedicationType.Antiplatelet,
            Dosage = 81.0m,
            Frequency = MedicationFrequency.OnceDaily
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.InternalServerError,
            "Internal server error");

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CreateMedicationAsync(medication);

        // Assert
        Assert.Null(result);
        Assert.Contains(_snackbarMessages, msg => msg.Contains("Failed to create medication"));
    }

    [Fact]
    public async Task CreateMedicationAsync_WithSuccessfulResponse_ReturnsCreatedMedication()
    {
        // Arrange
        var medication = new Medication
        {
            Name = "Test Aspirin",
            Type = MedicationType.Antiplatelet,
            Dosage = 81.0m,
            Frequency = MedicationFrequency.OnceDaily
        };

        var createdMedication = new Medication
        {
            Id = 1,
            Name = medication.Name,
            Type = medication.Type,
            Dosage = medication.Dosage,
            Frequency = medication.Frequency,
            IsActive = true
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.Created,
            JsonSerializer.Serialize(createdMedication));

        var service = new MedicationService(
            httpClient,
            _mockSnackbar.Object,
            _mockLogger.Object);

        // Act
        var result = await service.CreateMedicationAsync(medication);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdMedication.Id, result.Id);
        Assert.Equal(createdMedication.Name, result.Name);
        // Success case shows a success message, which is correct UX
        Assert.Contains(_snackbarMessages, msg => msg.Contains("created successfully"));
    }

    /// <summary>
    /// Creates a mock HttpClient that returns the specified status code and content.
    /// </summary>
    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });

        return new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
    }
}
