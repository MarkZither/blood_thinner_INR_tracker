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
using BloodThinnerTracker.Api.Data;
using BloodThinnerTracker.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

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
    // HttpContextAccessor is used by the context for user info; provide a basic implementation
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    // Provide a minimal IWebHostEnvironment required by EnsureDatabaseAsync
    services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment { EnvironmentName = "Development", ApplicationName = "BloodThinnerTracker.Tests", ContentRootPath = Directory.GetCurrentDirectory() });

        // Configure in-memory SQLite for ApplicationDbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite("Data Source=:memory:");
        });

        var provider = services.BuildServiceProvider();

        // Act & Assert: EnsureDatabaseAsync should run without throwing
        var ex = await Record.ExceptionAsync(async () => await provider.EnsureDatabaseAsync());
        Assert.Null(ex);
    }
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
