using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BloodThinnerTracker.Data.Shared;

namespace BloodThinnerTracker.Api.Middleware;

/// <summary>
/// Middleware that sets the DbContext.CurrentUserId for any DbContext instances
/// resolved from the request scope. This ensures the DbContext has the current
/// user id available for audit fields without relying on constructor DI.
/// </summary>
public class DbContextCurrentUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DbContextCurrentUserMiddleware> _logger;

    public DbContextCurrentUserMiddleware(RequestDelegate next, ILogger<DbContextCurrentUserMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var currentUserService = context.RequestServices.GetService<ICurrentUserService>();
            var userId = currentUserService?.GetCurrentUserId();

            // Resolve any DbContext instances in this scope and set CurrentUserId if they are our ApplicationDbContextBase
            var dbContexts = context.RequestServices.GetServices<DbContext>();
            foreach (var baseCtx in dbContexts.OfType<ApplicationDbContextBase>())
            {
                baseCtx.CurrentUserId = userId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set CurrentUserId on DbContext instances");
        }

        await _next(context);
    }
}
