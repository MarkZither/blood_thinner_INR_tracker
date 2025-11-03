using BloodThinnerTracker.Data.Shared;

namespace BloodThinnerTracker.Data.PostgreSQL;

/// <summary>
/// Design-time implementation of ICurrentUserService for EF Core migrations.
/// Returns null since there's no authenticated user during migration generation.
/// </summary>
internal class DesignTimeCurrentUserService : ICurrentUserService
{
    public int? GetCurrentUserId() => null;
}
