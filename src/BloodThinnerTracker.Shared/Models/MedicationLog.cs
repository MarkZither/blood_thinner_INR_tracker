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
    /// </summary>
    [Required]
    public Guid MedicationId { get; set; }        /// <summary>
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