using BloodThinnerTracker.Data.Shared;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Data.PostgreSQL;

/// <summary>
/// PostgreSQL-specific implementation of ApplicationDbContext.
/// Inherits all configuration from ApplicationDbContextBase.
/// </summary>
public class ApplicationDbContext : ApplicationDbContextBase
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDataProtectionProvider dataProtectionProvider,
        ICurrentUserService currentUserService,
        ILogger<ApplicationDbContext> logger)
        : base(options, dataProtectionProvider, currentUserService, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL doesn't support 'nvarchar' - convert to 'character varying'
        // This overrides the [Column(TypeName = "nvarchar(...)")] attributes from the shared models
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var columnType = property.GetColumnType();
                if (!string.IsNullOrEmpty(columnType) && columnType.StartsWith("nvarchar", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the length from nvarchar(N) and convert to character varying(N)
                    var openParenIndex = columnType.IndexOf('(');
                    var closeParenIndex = columnType.IndexOf(')', openParenIndex + 1);
                    if (openParenIndex != -1 && closeParenIndex != -1 && closeParenIndex > openParenIndex)
                    {
                        var length = columnType.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
                        property.SetColumnType($"character varying({length})");
                    }
                    else
                    {
                        property.SetColumnType("character varying");
                    }
                }
            }
        }
    }
}
