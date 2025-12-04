using System;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// Simple centralized error handling helper for the Mobile project.
/// Provides a place to add platform-specific logging/telemetry hooks later.
/// </summary>
public static class ErrorHandling
{
    public static void HandleException(Exception ex, ILogger? logger = null, string? context = null)
    {
        logger?.LogError(ex, "Unhandled exception{Context}", context == null ? string.Empty : $" in {context}");
        // TODO: Add telemetry export, user-friendly reporting UI, and optionally rethrow for higher-level handling
    }
}
