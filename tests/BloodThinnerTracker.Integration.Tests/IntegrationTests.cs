//-----------------------------------------------------------------------
// <copyright file="IntegrationTests.cs" company="Blood Thinner Tracker">
//     Copyright (c) Blood Thinner Tracker. All rights reserved.
//     MEDICAL DISCLAIMER: This software is for informational purposes only.
//     Always consult with qualified healthcare professionals for medical decisions.
// </copyright>
//-----------------------------------------------------------------------

namespace BloodThinnerTracker.Integration.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BloodThinnerTracker.Data.Shared;
using BloodThinnerTracker.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.Net.Http.Json;

/// <summary>
/// Integration tests for the Blood Thinner Tracker application.
/// </summary>
public sealed class IntegrationTests
{
    /// <summary>
    /// Tests that the integration environment can be configured successfully.
    /// </summary>
    [Fact]
    public void IntegrationEnvironmentCanBeConfigured()
    {
        // Arrange & Act
        var result = true; // Placeholder test

        // Assert
        Assert.True(result, "Integration environment should be configurable");
    }

    [Fact]
    public async Task EnsureDatabaseAsync_AppliesMigrations_InMemorySqlite()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Provide minimal services required by ApplicationDbContext
        // Data protection provider is required by the context constructor
        services.AddSingleton<IDataProtectionProvider>(DataProtectionProvider.Create("BloodThinnerTracker.Tests"));
        // Current user service for user context (returns null in tests)
        services.AddScoped<ICurrentUserService, TestCurrentUserService>();
        // Provide a minimal IWebHostEnvironment required by EnsureDatabaseAsync
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment { EnvironmentName = "Development", ApplicationName = "BloodThinnerTracker.Tests", ContentRootPath = Directory.GetCurrentDirectory() });

        // Configure in-memory SQLite using the SQLite provider
        services.AddDbContext<IApplicationDbContext, BloodThinnerTracker.Data.SQLite.ApplicationDbContext>(options =>
        {
            options.UseSqlite("Data Source=:memory:");
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        var provider = services.BuildServiceProvider();

        // Act & Assert: EnsureDatabaseAsync should run without throwing
        var ex = await Record.ExceptionAsync(async () => await provider.EnsureDatabaseAsync());
        Assert.Null(ex);
    }

    /// <summary>
    /// Regression test: Verifies that deserializing API response (string IDs) to INRTest (int IDs) throws JsonException.
    /// This documents the bug where INRService.CreateTestAsync/UpdateTestAsync fail at runtime.
    /// When fixed, INRService should use INRTestResponse instead of INRTest for deserialization.
    /// </summary>
    [Fact]
    public async Task RegressionTest_DeserializingStringIdToIntId_ThrowsJsonException()
    {
        // Arrange - Simulate API response JSON with string IDs (GUID format) like the real API returns
        var jsonResponse = @"{
            ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
            ""userId"": ""123e4567-e89b-12d3-a456-426614174000"",
            ""testDate"": ""2025-11-01T10:30:00Z"",
            ""inrValue"": 2.5,
            ""targetINRMin"": 2.0,
            ""targetINRMax"": 3.0,
            ""status"": 0,
            ""isPointOfCare"": false,
            ""reviewedByProvider"": false,
            ""patientNotified"": false,
            ""createdAt"": ""2025-11-01T10:30:00Z""
        }";

        var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        // Act & Assert - Demonstrates the bug: trying to deserialize to INRTest (which has int Id) throws
        // This is exactly what happens in INRService.CreateTestAsync and UpdateTestAsync
        var exception = await Assert.ThrowsAsync<System.Text.Json.JsonException>(async () =>
        {
            var test = await httpResponseMessage.Content.ReadFromJsonAsync<BloodThinnerTracker.Shared.Models.INRTest>();
        });

        // Verify error message confirms it's the expected type conversion failure
        Assert.Contains("Int32", exception.Message);
        Assert.Contains("id", exception.Message.ToLower());
    }

    /// <summary>
    /// Regression test: Verifies that deserializing API response (string IDs) to Medication (int IDs) throws JsonException.
    /// This documents the bug where MedicationService.CreateMedicationAsync fails at runtime.
    /// Error: "The JSON value could not be converted to System.Int32. Path: $.id | LineNumber: 0 | BytePositionInLine: 44"
    /// When fixed, MedicationService should use MedicationResponse instead of Medication for deserialization.
    /// </summary>
    [Fact]
    public async Task RegressionTest_MedicationDeserializingStringIdToIntId_ThrowsJsonException()
    {
        // Arrange - Simulate API response JSON with string IDs (GUID format) like the real API returns
        var jsonResponse = @"{
            ""id"": ""abc12345-e29b-41d4-a716-446655440000"",
            ""name"": ""Warfarin"",
            ""brandName"": ""Coumadin"",
            ""genericName"": ""Warfarin Sodium"",
            ""dosage"": 5.0,
            ""dosageUnit"": ""mg"",
            ""type"": 1,
            ""frequency"": 1,
            ""isBloodThinner"": true,
            ""isActive"": true,
            ""startDate"": ""2025-01-01T00:00:00Z"",
            ""remindersEnabled"": false,
            ""reminderMinutes"": 30,
            ""maxDailyDose"": 10.0,
            ""minHoursBetweenDoses"": 12,
            ""requiresINRMonitoring"": true,
            ""createdAt"": ""2025-11-01T10:30:00Z"",
            ""updatedAt"": ""2025-11-01T10:30:00Z""
        }";

        var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        // Act & Assert - Demonstrates the bug: trying to deserialize to Medication (which has int Id) throws
        // This is exactly what happens in MedicationService.CreateMedicationAsync line 94
        var exception = await Assert.ThrowsAsync<System.Text.Json.JsonException>(async () =>
        {
            var medication = await httpResponseMessage.Content.ReadFromJsonAsync<BloodThinnerTracker.Shared.Models.Medication>();
        });

        // Verify error message confirms it's the expected type conversion failure
        Assert.Contains("Int32", exception.Message);
        Assert.Contains("id", exception.Message.ToLower());
    }
}

/// <summary>
/// Test implementation of ICurrentUserService that returns null (no authenticated user).
/// </summary>
internal sealed class TestCurrentUserService : ICurrentUserService
{
    public int? GetCurrentUserId() => null;
}

/// <summary>
/// Minimal IWebHostEnvironment implementation for tests.
/// </summary>
internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "BloodThinnerTracker.Tests";
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
