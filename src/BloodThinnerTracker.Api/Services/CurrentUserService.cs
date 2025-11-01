using BloodThinnerTracker.Data.Shared;
using System.Security.Claims;

namespace BloodThinnerTracker.Api.Services;

/// <summary>
/// Production implementation of ICurrentUserService that extracts user ID from HTTP context.
/// Used by the data layer for audit logging without coupling to ASP.NET Core.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current authenticated user's internal database ID from JWT claims.
    /// Returns null if no user is authenticated (e.g., during migrations, background jobs, or anonymous requests).
    /// </summary>
    public int? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}
