using System.Collections.Generic;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Models.V1
{
    /// <summary>
    /// Response containing pattern history with pagination metadata.
    /// </summary>
    public sealed class PatternHistoryResponse
    {
        public int MedicationId { get; set; }
        public int TotalCount { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        public List<DosagePatternResponse> Patterns { get; set; } = new();
    }
}
