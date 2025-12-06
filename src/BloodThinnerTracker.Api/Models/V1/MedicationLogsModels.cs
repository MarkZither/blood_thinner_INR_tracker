using System.ComponentModel.DataAnnotations;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Models.V1
{
    /// <summary>
    /// Response model for medication log data.
    /// </summary>
    public sealed class MedicationLogResponse
    {
        public Guid PublicId { get; set; }
        public Guid MedicationPublicId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public DateTime? ActualTime { get; set; }
        public MedicationLogStatus Status { get; set; }
        public decimal? ActualDosage { get; set; }
        public string? ActualDosageUnit { get; set; }
        public string? Reason { get; set; }
        public string? SideEffects { get; set; }
        public string? Notes { get; set; }
        public bool? TakenWithFood { get; set; }
        public string? FoodDetails { get; set; }
        public int TimeVarianceMinutes { get; set; }
        public DateTime CreatedAt { get; set; }

        // Variance tracking fields (T035)
        /// <summary>
        /// Expected dosage from the active pattern on ScheduledTime date.
        /// NULL if no pattern was active.
        /// </summary>
        public decimal? ExpectedDosage { get; set; }

        /// <summary>
        /// Position in the dosage pattern cycle (1-based).
        /// Example: Day 3 of a 6-day pattern.
        /// NULL if no pattern was active.
        /// </summary>
        public int? PatternDayNumber { get; set; }

        /// <summary>
        /// Indicates whether actual dosage differs from expected dosage (variance > 0.01mg).
        /// </summary>
        public bool HasVariance { get; set; }

        /// <summary>
        /// Variance amount (actual - expected).
        /// Positive = took more than expected, negative = took less.
        /// NULL if no expected dosage is set.
        /// </summary>
        public decimal? VarianceAmount { get; set; }

        /// <summary>
        /// Variance percentage ((actual - expected) / expected * 100).
        /// Example: -25% means took 25% less than expected.
        /// NULL if no expected dosage or expected dosage is 0.
        /// </summary>
        public decimal? VariancePercentage { get; set; }
    }

    /// <summary>
    /// Request model for logging a medication dose.
    /// </summary>
    public sealed class LogMedicationRequest
    {
        [Required]
        public Guid MedicationId { get; set; }

        public DateTime? ScheduledTime { get; set; }

        public DateTime? ActualTime { get; set; }

        public MedicationLogStatus? Status { get; set; }

        [Range(0.01, 1000)]
        public decimal? ActualDosage { get; set; }

        [StringLength(20)]
        public string? ActualDosageUnit { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(500)]
        public string? SideEffects { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool? TakenWithFood { get; set; }

        [StringLength(200)]
        public string? FoodDetails { get; set; }
    }

    /// <summary>
    /// Request model for updating a medication log.
    /// </summary>
    public sealed class UpdateMedicationLogRequest
    {
        public DateTime? ActualTime { get; set; }

        public MedicationLogStatus? Status { get; set; }

        [Range(0.01, 1000)]
        public decimal? ActualDosage { get; set; }

        [StringLength(20)]
        public string? ActualDosageUnit { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(500)]
        public string? SideEffects { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool? TakenWithFood { get; set; }

        [StringLength(200)]
        public string? FoodDetails { get; set; }
    }

    /// <summary>
    /// Result of medication log safety validation.
    /// </summary>
    internal sealed class SafetyValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
