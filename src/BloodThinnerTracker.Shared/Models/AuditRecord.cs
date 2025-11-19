using System;

namespace BloodThinnerTracker.Shared.Models
{
    /// <summary>
    /// Represents an audit record capturing before/after JSON for entity changes.
    /// </summary>
    public class AuditRecord
    {
        /// <summary>
        /// Primary key (internal).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Public id of the audited entity (if available).
        /// </summary>
        public Guid? EntityPublicId { get; set; }

        /// <summary>
        /// The entity type name (e.g., "INRTest").
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// The user public id who performed the change.
        /// </summary>
        public Guid? PerformedBy { get; set; }

        /// <summary>
        /// Time of the change (UTC).
        /// </summary>
        public DateTime OccurredAtUtc { get; set; }

        /// <summary>
        /// JSON snapshot of entity before change. Null for creates.
        /// </summary>
        public string? BeforeJson { get; set; }

        /// <summary>
        /// JSON snapshot of entity after change. Null for deletes.
        /// </summary>
        public string? AfterJson { get; set; }
    }
}
