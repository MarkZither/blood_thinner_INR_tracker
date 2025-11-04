// BloodThinnerTracker.Shared - Medication Log Entity for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Medication log entity for tracking when medications are taken.
    /// Records actual medication intake events for compliance monitoring.
    ///
    /// MEDICAL DISCLAIMER: This entity tracks medication adherence.
    /// Always follow prescribed medication schedules and consult healthcare providers.
    /// </summary>
    [Table("MedicationLogs")]
public class MedicationLog : MedicalEntityBase
{
        /// <summary>
        /// Gets or sets the related medication identifier.
        /// ⚠️ SECURITY: This is an internal foreign key. Use Medication.PublicId when exposing via API.
        /// </summary>
        [Required]
        public int MedicationId { get; set; }

        /// <summary>
        /// Gets or sets the scheduled time when medication should have been taken.
        /// </summary>
        [Required]
        public DateTime ScheduledTime { get; set; }

        /// <summary>
        /// Gets or sets the actual time when medication was taken.
        /// </summary>
        public DateTime? ActualTime { get; set; }

        /// <summary>
        /// Gets or sets the status of this medication log entry.
        /// </summary>
        [Required]
        public MedicationLogStatus Status { get; set; } = MedicationLogStatus.Scheduled;

        /// <summary>
        /// Gets or sets the actual dosage taken (may differ from prescribed).
        /// </summary>
        [Range(0.0, 1000.0, ErrorMessage = "Dosage must be between 0 and 1000")]
        [Column(TypeName = "decimal(10,3)")]
        public decimal? ActualDosage { get; set; }

        /// <summary>
        /// Gets or sets the expected dosage from the active pattern on TakenAt/ScheduledTime date.
        /// NULL if no pattern was active or medication doesn't use patterns.
        /// </summary>
        /// <remarks>
        /// ⚠️ VARIANCE TRACKING: Populated automatically when log is created.
        ///
        /// Algorithm:
        /// 1. On log creation, call Medication.GetExpectedDosageForDate(ScheduledTime)
        /// 2. Store result in this field
        /// 3. If pattern exists, also populate PatternDayNumber and DosagePatternId
        /// 4. If no pattern, this field remains NULL (backward compatible)
        ///
        /// Used for:
        /// - Variance calculations (ActualDosage - ExpectedDosage)
        /// - Adherence reports ("Took 3mg instead of expected 4mg")
        /// - Historical accuracy (if pattern changes later, this preserves original expectation)
        /// </remarks>
        [Column(TypeName = "decimal(10,3)")]
        public decimal? ExpectedDosage { get; set; }

        /// <summary>
        /// Gets or sets the position in the dosage pattern cycle (1-based).
        /// Example: Day 3 of a 6-day pattern.
        /// NULL if no pattern was active.
        /// </summary>
        /// <remarks>
        /// ⚠️ PATTERN POSITION: Helps users understand their position in dosing cycle.
        ///
        /// Calculation:
        /// - daysSinceStart = (ScheduledTime.Date - Pattern.StartDate.Date).Days
        /// - PatternDayNumber = (daysSinceStart % Pattern.PatternLength) + 1
        ///
        /// Used for:
        /// - UI display: "Day 3 of 6 in pattern"
        /// - Pattern adherence tracking
        /// - Debugging dosage calculation issues
        /// </remarks>
        public int? PatternDayNumber { get; set; }

        /// <summary>
        /// Gets or sets the reference to the dosage pattern that was active on TakenAt date.
        /// Enables historical pattern lookup even if pattern is later modified/closed.
        /// NULL if no pattern was active.
        /// </summary>
        /// <remarks>
        /// ⚠️ TEMPORAL ACCURACY: Critical for FR-013 (historical pattern tracking).
        ///
        /// Why store PatternId instead of just calculating on-the-fly:
        /// - Pattern may be closed/modified after log is created
        /// - Need to know WHICH pattern was active at time of logging
        /// - Enables accurate variance reports across pattern changes
        /// - Preserves audit trail for medical compliance
        ///
        /// Example: User logs dose on Nov 1 with Pattern A (4,4,3).
        /// On Nov 2, pattern changes to Pattern B (4,4,3,4,3,3).
        /// Nov 1 log should still reference Pattern A, not Pattern B.
        /// </remarks>
        public int? DosagePatternId { get; set; }

        /// <summary>
        /// Gets or sets the dosage unit for actual dosage.
        /// </summary>
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? ActualDosageUnit { get; set; }

        /// <summary>
        /// Gets or sets the reason if medication was skipped or modified.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? Reason { get; set; }

        /// <summary>
        /// Gets or sets side effects experienced with this dose.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? SideEffects { get; set; }

        /// <summary>
        /// Gets or sets additional notes about this medication event.
        /// </summary>
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the method used to record this log entry.
        /// </summary>
        [Required]
        public LogEntryMethod EntryMethod { get; set; } = LogEntryMethod.Manual;

        /// <summary>
        /// Gets or sets the device or application used to log this entry.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? EntryDevice { get; set; }

        /// <summary>
        /// Gets or sets the geographical location when medication was taken (for travel tracking).
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? Location { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entry was confirmed by a healthcare provider.
        /// </summary>
        public bool ConfirmedByProvider { get; set; } = false;

        /// <summary>
        /// Gets or sets the healthcare provider who confirmed this entry.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? ConfirmedBy { get; set; }

        /// <summary>
        /// Gets or sets when this entry was confirmed.
        /// </summary>
        public DateTime? ConfirmedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this dose was taken with food.
        /// </summary>
        public bool? TakenWithFood { get; set; }

        /// <summary>
        /// Gets or sets food/drink consumed with medication (important for some blood thinners).
        /// </summary>
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string? FoodDetails { get; set; }

        /// <summary>
        /// Gets or sets the time window variance from scheduled time (in minutes).
        /// </summary>
        public int TimeVarianceMinutes { get; set; }

        // Navigation properties

        /// <summary>
        /// Gets or sets the user who took the medication.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the medication that was taken.
        /// </summary>
        [ForeignKey("MedicationId")]
        public virtual Medication Medication { get; set; } = null!;

        /// <summary>
        /// Gets or sets the dosage pattern that was active when this log was created.
        /// NULL if medication didn't use patterns at the time.
        /// </summary>
        /// <remarks>
        /// ⚠️ NAVIGATION PROPERTY: Virtual for EF Core lazy loading.
        /// OnDelete: SetNull (if pattern is deleted, logs remain with NULL reference)
        /// </remarks>
        [ForeignKey("DosagePatternId")]
        public virtual MedicationDosagePattern? DosagePattern { get; set; }

        // Computed Properties for Variance Tracking

        /// <summary>
        /// Gets a value indicating whether actual dosage differs from expected dosage.
        /// Threshold: > 0.01mg difference to account for decimal rounding.
        /// </summary>
        /// <remarks>
        /// ⚠️ VARIANCE DETECTION: Used for UI indicators and adherence reports.
        ///
        /// Returns true if:
        /// - ExpectedDosage is set (pattern was active)
        /// - AND absolute difference > 0.01mg
        ///
        /// Examples:
        /// - Expected 4mg, took 4mg → false (no variance)
        /// - Expected 4mg, took 3mg → true (variance)
        /// - Expected 4mg, took 4.005mg → false (within rounding threshold)
        /// - No expected dosage → false (backward compatible)
        /// </remarks>
        [NotMapped]
        public bool HasVariance => ExpectedDosage.HasValue &&
                                   ActualDosage.HasValue &&
                                   Math.Abs(ActualDosage.Value - ExpectedDosage.Value) > 0.01m;

        /// <summary>
        /// Gets the variance amount (actual - expected).
        /// Positive = took more than expected, negative = took less.
        /// NULL if no expected dosage is set.
        /// </summary>
        /// <remarks>
        /// ⚠️ VARIANCE CALCULATION: Signed decimal for UI display.
        ///
        /// Examples:
        /// - Expected 4mg, took 5mg → +1.0 (took 1mg more)
        /// - Expected 4mg, took 3mg → -1.0 (took 1mg less)
        /// - Expected 4mg, no actual → NULL (not yet taken)
        ///
        /// Used for:
        /// - Variance reports (total over/under dosing)
        /// - Safety alerts (large variances)
        /// - Adherence scoring
        /// </remarks>
        [NotMapped]
        public decimal? VarianceAmount => ExpectedDosage.HasValue && ActualDosage.HasValue
            ? ActualDosage.Value - ExpectedDosage.Value
            : null;

        /// <summary>
        /// Gets the variance percentage.
        /// Example: -25% means took 25% less than expected.
        /// NULL if no expected dosage or expected dosage is 0.
        /// </summary>
        /// <remarks>
        /// ⚠️ PERCENTAGE CALCULATION: For relative variance analysis.
        ///
        /// Formula: ((actual - expected) / expected) * 100
        ///
        /// Examples:
        /// - Expected 4mg, took 3mg → -25% (25% less)
        /// - Expected 4mg, took 5mg → +25% (25% more)
        /// - Expected 4mg, took 4mg → 0% (exact match)
        ///
        /// Used for:
        /// - Percentage-based adherence thresholds
        /// - Visual indicators (red if >25% variance)
        /// - Trend analysis (consistent under/over dosing)
        /// </remarks>
        [NotMapped]
        public decimal? VariancePercentage => ExpectedDosage.HasValue &&
                                              ExpectedDosage.Value > 0 &&
                                              ActualDosage.HasValue
            ? ((ActualDosage.Value - ExpectedDosage.Value) / ExpectedDosage.Value) * 100
            : null;

        /// <summary>
        /// Calculate if medication was taken on time (within acceptable window).
        /// </summary>
        /// <param name="acceptableWindowMinutes">Acceptable time window in minutes (default: 60).</param>
        /// <returns>True if taken within acceptable time window.</returns>
        public bool IsTakenOnTime(int acceptableWindowMinutes = 60)
        {
            if (!ActualTime.HasValue || Status != MedicationLogStatus.Taken)
                return false;

            var timeDifference = Math.Abs((ActualTime.Value - ScheduledTime).TotalMinutes);
            return timeDifference <= acceptableWindowMinutes;
        }

        /// <summary>
        /// Calculate adherence score for this medication log entry.
        /// </summary>
        /// <returns>Adherence score from 0.0 to 1.0.</returns>
        public double CalculateAdherenceScore()
        {
            return Status switch
            {
                MedicationLogStatus.Taken => IsTakenOnTime() ? 1.0 : 0.8,
                MedicationLogStatus.PartiallyTaken => 0.5,
                MedicationLogStatus.Skipped => 0.0,
                MedicationLogStatus.Scheduled => 0.0, // Not yet taken
                _ => 0.0
            };
        }

        /// <summary>
        /// Get time difference from scheduled time in a human-readable format.
        /// </summary>
        /// <returns>Human-readable time difference.</returns>
        public string GetTimeDifferenceDescription()
        {
            if (!ActualTime.HasValue)
                return Status == MedicationLogStatus.Scheduled ? "Not taken yet" : "No time recorded";

            var difference = ActualTime.Value - ScheduledTime;
            var absDifference = Math.Abs(difference.TotalMinutes);

            if (absDifference < 1)
                return "On time";

            var direction = difference.TotalMinutes > 0 ? "late" : "early";

            if (absDifference < 60)
                return $"{(int)absDifference} minutes {direction}";

            var hours = (int)(absDifference / 60);
            var minutes = (int)(absDifference % 60);

            if (minutes == 0)
                return $"{hours} hour{(hours > 1 ? "s" : "")} {direction}";

            return $"{hours}h {minutes}m {direction}";
        }

        /// <summary>
        /// Validate medication log for medical safety.
        /// </summary>
        /// <returns>List of validation errors.</returns>
        public List<string> ValidateForMedicalSafety()
        {
            var errors = new List<string>();

            if (ScheduledTime > DateTime.UtcNow.AddDays(7))
                errors.Add("Scheduled time cannot be more than 7 days in the future");

            if (ActualTime.HasValue && ActualTime.Value > DateTime.UtcNow.AddMinutes(5))
                errors.Add("Actual time cannot be in the future");

            if (ActualDosage.HasValue && ActualDosage.Value <= 0)
                errors.Add("Actual dosage must be greater than 0 if specified");

            if (Status == MedicationLogStatus.Taken && !ActualTime.HasValue)
                errors.Add("Actual time is required when medication is marked as taken");

            if (Status == MedicationLogStatus.PartiallyTaken && !ActualDosage.HasValue)
                errors.Add("Actual dosage is required when medication is partially taken");

            // Medical safety: Check for potential double-dosing (within 12 hours)
            if (ActualTime.HasValue)
            {
                var timeSinceScheduled = Math.Abs((ActualTime.Value - ScheduledTime).TotalHours);
                if (timeSinceScheduled > 12 && Status == MedicationLogStatus.Taken)
                    errors.Add("WARNING: Medication taken more than 12 hours from scheduled time - verify with healthcare provider");
            }

            return errors;
        }

        /// <summary>
        /// Populate pattern-related fields from medication's active pattern.
        /// Should be called when creating a new log entry to capture expected dosage.
        /// </summary>
        /// <param name="medication">The medication to get expected dosage from.</param>
        /// <exception cref="ArgumentNullException">Thrown if medication is null.</exception>
        /// <remarks>
        /// ⚠️ AUTO-POPULATION: Critical method for variance tracking (FR-009, FR-010).
        ///
        /// Algorithm:
        /// 1. Call medication.GetExpectedDosageForDate(ScheduledTime) to get pattern dosage
        /// 2. Store result in ExpectedDosage field
        /// 3. If pattern exists, also populate:
        ///    - DosagePatternId (FK to active pattern)
        ///    - PatternDayNumber (position in pattern cycle)
        /// 4. If no pattern, leave fields NULL (backward compatible)
        ///
        /// Usage:
        /// ```csharp
        /// var log = new MedicationLog
        /// {
        ///     MedicationId = medication.Id,
        ///     ScheduledTime = DateTime.UtcNow
        /// };
        /// log.SetExpectedDosageFromMedication(medication);
        /// // Now log.ExpectedDosage is set, log.PatternDayNumber is set
        /// ```
        ///
        /// Called by:
        /// - POST /api/medication-logs endpoint (T033)
        /// - LogDose page when creating new log (T038)
        /// - Medication reminder service (background job)
        /// </remarks>
        public void SetExpectedDosageFromMedication(Medication medication)
        {
            if (medication == null)
                throw new ArgumentNullException(nameof(medication));

            // Use ScheduledTime as the target date for pattern lookup
            ExpectedDosage = medication.GetExpectedDosageForDate(ScheduledTime);

            // If pattern exists, populate pattern tracking fields
            var activePattern = medication.GetPatternForDate(ScheduledTime);
            if (activePattern != null)
            {
                DosagePatternId = activePattern.Id;

                // Calculate pattern day number (1-based)
                int daysSinceStart = (ScheduledTime.Date - activePattern.StartDate.Date).Days;
                PatternDayNumber = (daysSinceStart % activePattern.PatternLength) + 1;
            }
            else
            {
                // No pattern active - clear pattern fields
                DosagePatternId = null;
                PatternDayNumber = null;
            }
        }

        /// <summary>
        /// Mark medication as taken with current timestamp.
        /// </summary>
        /// <param name="actualDosage">Actual dosage taken (optional).</param>
        /// <param name="notes">Additional notes (optional).</param>
        public void MarkAsTaken(decimal? actualDosage = null, string? notes = null)
        {
            Status = MedicationLogStatus.Taken;
            ActualTime = DateTime.UtcNow;
            ActualDosage = actualDosage;
            Notes = notes;
            TimeVarianceMinutes = (int)(ActualTime.Value - ScheduledTime).TotalMinutes;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Mark medication as skipped with reason.
        /// </summary>
        /// <param name="reason">Reason for skipping medication.</param>
        public void MarkAsSkipped(string reason)
        {
            Status = MedicationLogStatus.Skipped;
            Reason = reason;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Status options for medication log entries.
    /// </summary>
    public enum MedicationLogStatus
    {
        /// <summary>
        /// Medication is scheduled but not yet taken.
        /// </summary>
        Scheduled = 0,

        /// <summary>
        /// Medication was taken as prescribed.
        /// </summary>
        Taken = 1,

        /// <summary>
        /// Medication was skipped/not taken.
        /// </summary>
        Skipped = 2,

        /// <summary>
        /// Partial dose was taken.
        /// </summary>
        PartiallyTaken = 3,

        /// <summary>
        /// Medication time was rescheduled.
        /// </summary>
        Rescheduled = 4
    }

    /// <summary>
    /// Method used to create log entry.
    /// </summary>
    public enum LogEntryMethod
    {
        /// <summary>
        /// Manually entered by user.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// Automatically logged by reminder system.
        /// </summary>
        Automatic = 1,

        /// <summary>
        /// Entered by healthcare provider.
        /// </summary>
        Provider = 2,

        /// <summary>
        /// Imported from external device/system.
        /// </summary>
        Import = 3,

        /// <summary>
        /// Voice command entry.
        /// </summary>
        Voice = 4
    }
}
