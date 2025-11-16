// BloodThinnerTracker.Shared - INR Test Entity for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// INR (International Normalized Ratio) test entity for blood coagulation monitoring.
    /// Records INR test results for patients on blood thinner medications.
    /// 
    /// MEDICAL DISCLAIMER: INR values are critical for blood thinner safety.
    /// Always consult healthcare providers for interpretation and medication adjustments.
    /// </summary>
    [Table("INRTests")]
    public class INRTest : MedicalEntityBase
    {
        /// <summary>
        /// Gets or sets the date and time when the blood sample was taken.
        /// </summary>
        [Required]
        public DateTime TestDate { get; set; }

        /// <summary>
        /// Gets or sets the INR test result value.
        /// Normal range is typically 0.8-1.1, therapeutic range varies by condition.
        /// </summary>
        [Required]
        [Range(0.5, 8.0, ErrorMessage = "INR value must be between 0.5 and 8.0")]
        [Column(TypeName = "decimal(4,2)")]
        public decimal INRValue { get; set; }

        /// <summary>
        /// Gets or sets the target INR range minimum value.
        /// </summary>
        [Range(1.0, 4.0, ErrorMessage = "Target INR minimum must be between 1.0 and 4.0")]
        [Column(TypeName = "decimal(3,1)")]
        public decimal? TargetINRMin { get; set; }

        /// <summary>
        /// Gets or sets the target INR range maximum value.
        /// </summary>
        [Range(1.5, 4.5, ErrorMessage = "Target INR maximum must be between 1.5 and 4.5")]
        [Column(TypeName = "decimal(3,1)")]
        public decimal? TargetINRMax { get; set; }

        /// <summary>
        /// Gets or sets the PT (Prothrombin Time) in seconds.
        /// </summary>
        [Range(8.0, 60.0, ErrorMessage = "PT must be between 8 and 60 seconds")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? ProthrombinTime { get; set; }

        /// <summary>
        /// Gets or sets the PTT (Partial Thromboplastin Time) in seconds.
        /// </summary>
        [Range(20.0, 120.0, ErrorMessage = "PTT must be between 20 and 120 seconds")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? PartialThromboplastinTime { get; set; }

        /// <summary>
        /// Gets or sets the testing laboratory or facility.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? Laboratory { get; set; }

        /// <summary>
        /// Gets or sets the healthcare provider who ordered the test.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? OrderedBy { get; set; }

        /// <summary>
        /// Gets or sets the testing method or device used.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? TestMethod { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a point-of-care test.
        /// </summary>
        public bool IsPointOfCare { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the patient was fasting.
        /// </summary>
        public bool? WasFasting { get; set; }

        /// <summary>
        /// Gets or sets the time of last medication dose before test.
        /// </summary>
        public DateTime? LastMedicationTime { get; set; }

        /// <summary>
        /// Gets or sets medications taken before the test that might affect results.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? MedicationsTaken { get; set; }

        /// <summary>
        /// Gets or sets foods consumed that might affect INR (e.g., vitamin K foods).
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? FoodsConsumed { get; set; }

        /// <summary>
        /// Gets or sets illness or health conditions that might affect results.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? HealthConditions { get; set; }

        /// <summary>
        /// Gets or sets the result interpretation status.
        /// </summary>
        [Required]
        public INRResultStatus Status { get; set; } = INRResultStatus.InRange;

        /// <summary>
        /// Gets or sets recommended actions based on the result.
        /// </summary>
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? RecommendedActions { get; set; }

        /// <summary>
        /// Gets or sets medication dosage changes recommended.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? DosageChanges { get; set; }

        /// <summary>
        /// Gets or sets the date for the next recommended INR test.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? NextTestDate { get; set; }

        /// <summary>
        /// Gets or sets additional notes about the test or results.
        /// </summary>
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this result was reviewed by a healthcare provider.
        /// </summary>
        public bool ReviewedByProvider { get; set; } = false;

        /// <summary>
        /// Gets or sets the healthcare provider who reviewed the result.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? ReviewedBy { get; set; }

        /// <summary>
        /// Gets or sets when this result was reviewed.
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Gets or sets the last editor's public id (nullable).
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the id of the user who soft-deleted this record (public id).
    /// </summary>
    public Guid? DeletedBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the patient was notified of results.
        /// </summary>
        public bool PatientNotified { get; set; } = false;

        /// <summary>
        /// Gets or sets how the patient was notified.
        /// </summary>
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? NotificationMethod { get; set; }

        // Navigation properties

        /// <summary>
        /// Gets or sets the user for whom this test was performed.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Check if INR value is within target range.
        /// </summary>
        /// <returns>True if INR is within target range.</returns>
        public bool IsInTargetRange()
        {
            if (!TargetINRMin.HasValue || !TargetINRMax.HasValue)
                return false;

            return INRValue >= TargetINRMin.Value && INRValue <= TargetINRMax.Value;
        }

        /// <summary>
        /// Calculate how far the INR is from target range center.
        /// </summary>
        /// <returns>Deviation from target center, or null if no target range.</returns>
        public decimal? GetDeviationFromTarget()
        {
            if (!TargetINRMin.HasValue || !TargetINRMax.HasValue)
                return null;

            var targetCenter = (TargetINRMin.Value + TargetINRMax.Value) / 2;
            return INRValue - targetCenter;
        }

        /// <summary>
        /// Get time in therapeutic range category.
        /// </summary>
        /// <returns>Therapeutic range category.</returns>
        public TherapeuticRangeCategory GetTherapeuticRangeCategory()
        {
            if (!TargetINRMin.HasValue || !TargetINRMax.HasValue)
                return TherapeuticRangeCategory.Unknown;

            if (INRValue < TargetINRMin.Value)
            {
                // Below therapeutic range - risk of clotting
                if (INRValue < TargetINRMin.Value * 0.8m)
                    return TherapeuticRangeCategory.CriticallyLow;
                return TherapeuticRangeCategory.BelowRange;
            }

            if (INRValue > TargetINRMax.Value)
            {
                // Above therapeutic range - risk of bleeding
                if (INRValue > TargetINRMax.Value * 1.5m)
                    return TherapeuticRangeCategory.CriticallyHigh;
                return TherapeuticRangeCategory.AboveRange;
            }

            return TherapeuticRangeCategory.InRange;
        }

        /// <summary>
        /// Get risk assessment based on INR value.
        /// </summary>
        /// <returns>Risk assessment string.</returns>
        public string GetRiskAssessment()
        {
            return GetTherapeuticRangeCategory() switch
            {
                TherapeuticRangeCategory.CriticallyLow => "CRITICAL: Very high risk of blood clots. Contact healthcare provider immediately.",
                TherapeuticRangeCategory.BelowRange => "WARNING: Increased risk of blood clots. Medication adjustment may be needed.",
                TherapeuticRangeCategory.InRange => "GOOD: INR is within therapeutic range.",
                TherapeuticRangeCategory.AboveRange => "WARNING: Increased risk of bleeding. Medication adjustment may be needed.",
                TherapeuticRangeCategory.CriticallyHigh => "CRITICAL: Very high risk of bleeding. Contact healthcare provider immediately.",
                _ => "INR interpretation requires healthcare provider review."
            };
        }

        /// <summary>
        /// Validate INR test for medical safety.
        /// </summary>
        /// <returns>List of validation errors.</returns>
        public List<string> ValidateForMedicalSafety()
        {
            var errors = new List<string>();

            if (TestDate > DateTime.UtcNow)
                errors.Add("Test date cannot be in the future");

            if (INRValue < 0.5m || INRValue > 8.0m)
                errors.Add("INR value is outside medically plausible range (0.5-8.0)");

            if (TargetINRMin.HasValue && TargetINRMax.HasValue)
            {
                if (TargetINRMin.Value >= TargetINRMax.Value)
                    errors.Add("Target INR minimum must be less than maximum");

                if (TargetINRMin.Value < 1.0m || TargetINRMax.Value > 4.5m)
                    errors.Add("Target INR range is outside typical therapeutic ranges");
            }

            if (ProthrombinTime.HasValue && (ProthrombinTime.Value < 8 || ProthrombinTime.Value > 60))
                errors.Add("Prothrombin Time is outside normal range (8-60 seconds)");

            if (PartialThromboplastinTime.HasValue && (PartialThromboplastinTime.Value < 20 || PartialThromboplastinTime.Value > 120))
                errors.Add("Partial Thromboplastin Time is outside normal range (20-120 seconds)");

            // Critical safety checks
            if (INRValue >= 5.0m)
                errors.Add("CRITICAL: INR ≥ 5.0 requires immediate medical attention");

            if (INRValue <= 1.0m && TargetINRMin.HasValue && TargetINRMin.Value > 2.0m)
                errors.Add("CRITICAL: Very low INR for patient requiring anticoagulation");

            return errors;
        }

        /// <summary>
        /// Get display string for INR result with status.
        /// </summary>
        /// <returns>Formatted INR result string.</returns>
        public string GetDisplayResult()
        {
            var result = $"INR: {INRValue:F2}";
            
            if (TargetINRMin.HasValue && TargetINRMax.HasValue)
                result += $" (Target: {TargetINRMin:F1}-{TargetINRMax:F1})";

            var category = GetTherapeuticRangeCategory();
            result += category switch
            {
                TherapeuticRangeCategory.InRange => " ✓",
                TherapeuticRangeCategory.BelowRange or TherapeuticRangeCategory.AboveRange => " ⚠",
                TherapeuticRangeCategory.CriticallyLow or TherapeuticRangeCategory.CriticallyHigh => " 🚨",
                _ => ""
            };

            return result;
        }
    }

    /// <summary>
    /// INR result status options.
    /// </summary>
    public enum INRResultStatus
    {
        /// <summary>
        /// INR is within therapeutic range.
        /// </summary>
        InRange = 0,

        /// <summary>
        /// INR is below therapeutic range.
        /// </summary>
        BelowRange = 1,

        /// <summary>
        /// INR is above therapeutic range.
        /// </summary>
        AboveRange = 2,

        /// <summary>
        /// INR is critically low (immediate attention needed).
        /// </summary>
        CriticallyLow = 3,

        /// <summary>
        /// INR is critically high (immediate attention needed).
        /// </summary>
        CriticallyHigh = 4,

        /// <summary>
        /// Result pending review.
        /// </summary>
        PendingReview = 5
    }

    /// <summary>
    /// Therapeutic range categories for INR values.
    /// </summary>
    public enum TherapeuticRangeCategory
    {
        /// <summary>
        /// Unknown - no target range defined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// INR is within therapeutic range.
        /// </summary>
        InRange = 1,

        /// <summary>
        /// INR is below therapeutic range.
        /// </summary>
        BelowRange = 2,

        /// <summary>
        /// INR is above therapeutic range.
        /// </summary>
        AboveRange = 3,

        /// <summary>
        /// INR is critically low.
        /// </summary>
        CriticallyLow = 4,

        /// <summary>
        /// INR is critically high.
        /// </summary>
        CriticallyHigh = 5
    }
}