//-----------------------------------------------------------------------
// <copyright file="ServiceDefaults.cs" company="Blood Thinner Tracker">
//     Copyright (c) Blood Thinner Tracker. All rights reserved.
//     MEDICAL DISCLAIMER: This software is for informational purposes only.
//     Always consult with qualified healthcare professionals for medical decisions.
// </copyright>
//-----------------------------------------------------------------------

namespace BloodThinnerTracker.ServiceDefaults;

/// <summary>
/// Provides default service configurations for the Blood Thinner Tracker application.
/// This class contains shared configuration settings used across all services.
/// </summary>
public static class ServiceDefaults
{
    /// <summary>
    /// Gets the default application name.
    /// </summary>
    public static string ApplicationName => "BloodThinnerTracker";

    /// <summary>
    /// Gets the default service version.
    /// </summary>
    public static string Version => "1.0.0";

    /// <summary>
    /// Gets the default connection string for the application database.
    /// </summary>
    public static string DefaultConnectionString => "Data Source=bloodtracker.db";

    /// <summary>
    /// Gets the default JWT secret key for development.
    /// NOTE: In production, this should be configured via environment variables or secure configuration.
    /// </summary>
    public static string DefaultJwtSecret => "BloodTrackerSecretKey2024-ChangeInProduction";

    /// <summary>
    /// Gets the default CORS policy name.
    /// </summary>
    public static string DefaultCorsPolicy => "BloodTrackerCorsPolicy";
}
