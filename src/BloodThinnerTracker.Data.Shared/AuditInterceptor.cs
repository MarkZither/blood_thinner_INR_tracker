using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Data.Shared
{
    /// <summary>
    /// EF Core SaveChanges interceptor that records AuditRecord entries for tracked INRTest updates and soft-deletes.
    /// This implementation intentionally does NOT resolve a user service from the data layer.
    /// Higher-level services/controllers MUST set UpdatedBy/DeletedBy (public Guid) on entities before SaveChanges.
    /// AuditRecord entries are added to the same DbContext so they are persisted in the same transaction.
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is null) return base.SavingChanges(eventData, result);

            CaptureAuditRecords(eventData.Context);

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
            {
                CaptureAuditRecords(eventData.Context);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void CaptureAuditRecords(DbContext? context)
        {
            if (context is null) return;

            var entries = context.ChangeTracker.Entries<INRTest>()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted || e.State == EntityState.Added)
                .ToList();

            foreach (var entry in entries)
            {
                // Determine public id and performed-by from entity property values if present
                Guid? entityPublicId = null;
                Guid? performedBy = null;

                try
                {
                    if(entry.State != EntityState.Added)
                    {
                        if (entry.Properties.Any(p => p.Metadata.Name == nameof(INRTest.PublicId)))
                        {
                            object? cur = entry.CurrentValues[nameof(INRTest.PublicId)];
                            object? orig = entry.OriginalValues[nameof(INRTest.PublicId)];
                            entityPublicId = cur is Guid gcur ? gcur : (orig is Guid gorig ? gorig : (Guid?)null);
                        }

                        // Prefer UpdatedBy/DeletedBy properties on the entity for the actor
                        if (entry.Properties.Any(p => p.Metadata.Name == nameof(INRTest.UpdatedBy)))
                        {
                            object? cur = entry.CurrentValues[nameof(INRTest.UpdatedBy)];
                            object? orig = entry.OriginalValues[nameof(INRTest.UpdatedBy)];
                            performedBy = cur is Guid gcur ? gcur : (orig is Guid gorig ? gorig : (Guid?)null);
                        }

                        if (performedBy == null && entry.Properties.Any(p => p.Metadata.Name == nameof(INRTest.DeletedBy)))
                        {
                            object? cur = entry.CurrentValues[nameof(INRTest.DeletedBy)];
                            object? orig = entry.OriginalValues[nameof(INRTest.DeletedBy)];
                            performedBy = cur is Guid gcur ? gcur : (orig is Guid gorig ? gorig : (Guid?)null);
                        }
                    }
                }
                catch
                {
                    // Defensive: if property names differ in the EF model, leave values null
                    entityPublicId = null;
                    performedBy = null;
                }

                // For soft-delete flows we expect services/controllers to set IsDeleted=true instead of hard-deleting
                if (entry.State == EntityState.Deleted)
                {
                    // Convert to soft-delete audit: capture original and mark IsDeleted in after
                    var before = SerializeEntity(entry.OriginalValues);
                    var afterDict = entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => entry.CurrentValues[p.Name]);
                    afterDict["isDeleted"] = true;

                    var after = JsonSerializer.Serialize(afterDict, _jsonOptions);

                    var audit = new AuditRecord
                    {
                        EntityType = nameof(INRTest),
                        EntityPublicId = entityPublicId,
                        PerformedBy = performedBy,
                        OccurredAtUtc = DateTime.UtcNow,
                        BeforeJson = before,
                        AfterJson = after
                    };

                    context.Add(audit);
                }
                else if (entry.State == EntityState.Modified)
                {
                    var before = SerializeEntity(entry.OriginalValues);
                    var after = SerializeEntity(entry.CurrentValues);

                    // If IsDeleted changed from false->true, mark action as SoftDelete
                    var origIsDeleted = entry.OriginalValues.Properties.Any(p => p.Name == "IsDeleted") && (entry.OriginalValues["IsDeleted"] as bool? == true);
                    var currIsDeleted = entry.CurrentValues.Properties.Any(p => p.Name == "IsDeleted") && (entry.CurrentValues["IsDeleted"] as bool? == true);
                    // Action is encoded implicitly in AuditLog/record; keep the model simple and rely on metadata if needed

                    var audit = new AuditRecord
                    {
                        EntityType = nameof(INRTest),
                        EntityPublicId = entityPublicId,
                        PerformedBy = performedBy,
                        OccurredAtUtc = DateTime.UtcNow,
                        BeforeJson = before,
                        AfterJson = after
                    };

                    context.Add(audit);
                }
                else if (entry.State == EntityState.Added)
                {
                    // For completeness: record create as an AuditRecord with AfterJson
                    var after = SerializeEntity(entry.CurrentValues);

                    var audit = new AuditRecord
                    {
                        EntityType = nameof(INRTest),
                        EntityPublicId = entityPublicId ?? (entry.CurrentValues[nameof(INRTest.PublicId)] is Guid g ? g : (Guid?)null),
                        PerformedBy = performedBy,
                        OccurredAtUtc = DateTime.UtcNow,
                        BeforeJson = null,
                        AfterJson = after
                    };

                    context.Add(audit);
                }
            }
        }

        private string? SerializeEntity(PropertyValues? pv)
        {
            if (pv is null) return null;

            var dict = pv.Properties.ToDictionary(p => p.Name, p => pv[p.Name]);
            return JsonSerializer.Serialize(dict, _jsonOptions);
        }
    }
}
