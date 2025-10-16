//-----------------------------------------------------------------------
// <copyright file="SharedModels.cs" company="Blood Thinner Tracker">
//     Copyright (c) Blood Thinner Tracker. All rights reserved.
//     MEDICAL DISCLAIMER: This software is for informational purposes only.
//     Always consult with qualified healthcare professionals for medical decisions.
// </copyright>
//-----------------------------------------------------------------------

namespace BloodThinnerTracker.Shared;

/// <summary>
/// Contains shared models and contracts used across the Blood Thinner Tracker application.
/// </summary>
public static class SharedModels
{
    /// <summary>
    /// Gets the current API version.
    /// </summary>
    public static string ApiVersion => "v1";

    /// <summary>
    /// Gets the application display name.
    /// </summary>
    public static string DisplayName => "Blood Thinner & INR Tracker";
}
