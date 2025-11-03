namespace BloodThinnerTracker.Data.Shared;

/// <summary>
/// Service interface for getting the current authenticated user's ID.
/// This abstraction keeps the data layer independent of HTTP/ASP.NET Core concerns.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the internal database user ID of the currently authenticated user.
    /// Returns null if no user is authenticated (e.g., during seeding or background jobs).
    /// </summary>
    int? GetCurrentUserId();
}
