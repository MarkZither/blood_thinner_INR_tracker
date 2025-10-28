// BloodThinnerTracker.Shared - INR Schedule Entity for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// INR schedule entity for managing INR test scheduling and reminders.
    /// Tracks when INR tests should be performed based on medical protocols.
    /// 
    /// MEDICAL DISCLAIMER: INR testing frequency is determined by healthcare providers.
    /// Always follow prescribed testing schedules for blood thinner safety.
    /// </summary>
    [Table("INRSchedules")]
    public class INRSchedule : MedicalEntityBase
    {
        /// <summary>
        /// Gets or sets the scheduled date for the INR test.
        /// </summary>
        [Required]
        [DataType(DataType.Date)]
        public DateTime ScheduledDate { get; set; }

        /// <summary>
        /// Gets or sets the preferred time for INR testing.
        /// </summary>
        [DataType(DataType.Time)]
        public TimeSpan? PreferredTime { get; set; }

        /// <summary>
        /// Gets or sets the scheduling frequency pattern.
        /// </summary>
        [Required]
        public INRTestFrequency Frequency { get; set; }

        /// <summary>
        /// Gets or sets the interval in days for recurring schedules.
        /// </summary>
        [Range(1, 365, ErrorMessage = "Interval must be between 1 and 365 days")]
        public int IntervalDays { get; set; }

        /// <summary>
        /// Gets or sets the target INR range minimum for this schedule.
        /// </summary>
        [Range(1.0, 4.0, ErrorMessage = "Target INR minimum must be between 1.0 and 4.0")]
        [Column(TypeName = "decimal(3,1)")]
        public decimal? TargetINRMin { get; set; }

        /// <summary>
        /// Gets or sets the target INR range maximum for this schedule.
        /// </summary>
        [Range(1.5, 4.5, ErrorMessage = "Target INR maximum must be between 1.5 and 4.5")]
        [Column(TypeName = "decimal(3,1)")]
        public decimal? TargetINRMax { get; set; }

        /// <summary>
        /// Gets or sets the healthcare provider who set this schedule.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? PrescribedBy { get; set; }

        /// <summary>
        /// Gets or sets the date when this schedule was prescribed.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? PrescribedDate { get; set; }

        /// <summary>
        /// Gets or sets the preferred testing laboratory.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? PreferredLaboratory { get; set; }

        /// <summary>
        /// Gets or sets the laboratory contact information.
        /// </summary>
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string? LaboratoryContact { get; set; }

        /// <summary>
        /// Gets or sets special instructions for INR testing.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? TestingInstructions { get; set; }

        /// <summary>
        /// Gets or sets the schedule status.
        /// </summary>
        [Required]
        public INRScheduleStatus Status { get; set; } = INRScheduleStatus.Active;

        /// <summary>
        /// Gets or sets a value indicating whether reminders are enabled.
        /// </summary>
        public bool RemindersEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets reminder advance time in days.
        /// </summary>
        [Range(0, 14, ErrorMessage = "Reminder days must be between 0 and 14")]
        public int ReminderDays { get; set; } = 2;

        /// <summary>
        /// Gets or sets reminder methods (email, SMS, push notification).
        /// Stored as JSON array.
        /// </summary>
        [Column(TypeName = "nvarchar(200)")]
        public string? ReminderMethods { get; set; }

        /// <summary>
        /// Gets or sets the actual test date when completed.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Gets or sets the ID of the INR test that completed this schedule (if any).
        /// Links to the actual INR test result when the scheduled test is performed.
        /// </summary>
        public string? CompletedTestId { get; set; }

        /// <summary>
        /// Gets or sets the reason if schedule was modified or cancelled.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? ModificationReason { get; set; }

        /// <summary>
        /// Gets or sets the next scheduled date for recurring schedules.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? NextScheduledDate { get; set; }

        /// <summary>
        /// Gets or sets the end date for recurring schedules.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets additional notes about the schedule.
        /// </summary>
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this schedule was auto-generated.
        /// </summary>
        public bool IsAutoGenerated { get; set; } = false;

        /// <summary>
        /// Gets or sets the parent schedule ID for automatically generated recurring schedules.
        /// This creates a hierarchy of related INR test schedules for tracking patterns.
        /// </summary>
        public string? ParentScheduleId { get; set; }

        // Navigation properties

        /// <summary>
        /// Gets or sets the user for whom this schedule is created.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the completed INR test if applicable.
        /// </summary>
        [ForeignKey("CompletedTestId")]
        public virtual INRTest? CompletedTest { get; set; }

        /// <summary>
        /// Gets or sets the parent schedule for auto-generated schedules.
        /// </summary>
        [ForeignKey("ParentScheduleId")]
        public virtual INRSchedule? ParentSchedule { get; set; }

        /// <summary>
        /// Gets or sets child schedules generated from this schedule.
        /// </summary>
        public virtual ICollection<INRSchedule> ChildSchedules { get; set; } = new List<INRSchedule>();

        /// <summary>
        /// Check if schedule is currently due.
        /// </summary>
        /// <returns>True if schedule is due for testing.</returns>
        public bool IsDue()
        {
            if (Status != INRScheduleStatus.Active)
                return false;

            return ScheduledDate.Date <= DateTime.UtcNow.Date && !CompletedDate.HasValue;
        }

        /// <summary>
        /// Check if schedule is overdue.
        /// </summary>
        /// <param name="gracePeriodDays">Grace period in days (default: 3).</param>
        /// <returns>True if schedule is overdue.</returns>
        public bool IsOverdue(int gracePeriodDays = 3)
        {
            if (Status != INRScheduleStatus.Active || CompletedDate.HasValue)
                return false;

            return ScheduledDate.Date.AddDays(gracePeriodDays) < DateTime.UtcNow.Date;
        }

        /// <summary>
        /// Check if reminder should be sent.
        /// </summary>
        /// <returns>True if reminder should be sent.</returns>
        public bool ShouldSendReminder()
        {
            if (!RemindersEnabled || Status != INRScheduleStatus.Active || CompletedDate.HasValue)
                return false;

            var reminderDate = ScheduledDate.AddDays(-ReminderDays);
            return DateTime.UtcNow.Date >= reminderDate.Date && DateTime.UtcNow.Date <= ScheduledDate.Date;
        }

        /// <summary>
        /// Calculate days until scheduled test.
        /// </summary>
        /// <returns>Days until test (negative if overdue).</returns>
        public int DaysUntilTest()
        {
            return (ScheduledDate.Date - DateTime.UtcNow.Date).Days;
        }

        /// <summary>
        /// Generate next schedule based on frequency.
        /// </summary>
        /// <returns>Next INR schedule or null if no recurring pattern.</returns>
        public INRSchedule? GenerateNextSchedule()
        {
            if (Frequency == INRTestFrequency.OneTime || IntervalDays <= 0)
                return null;

            var nextDate = ScheduledDate.AddDays(IntervalDays);
            
            // Don't generate if past end date
            if (EndDate.HasValue && nextDate > EndDate.Value)
                return null;

            return new INRSchedule
            {
                UserId = UserId,
                ScheduledDate = nextDate,
                PreferredTime = PreferredTime,
                Frequency = Frequency,
                IntervalDays = IntervalDays,
                TargetINRMin = TargetINRMin,
                TargetINRMax = TargetINRMax,
                PrescribedBy = PrescribedBy,
                PreferredLaboratory = PreferredLaboratory,
                LaboratoryContact = LaboratoryContact,
                TestingInstructions = TestingInstructions,
                RemindersEnabled = RemindersEnabled,
                ReminderDays = ReminderDays,
                ReminderMethods = ReminderMethods,
                EndDate = EndDate,
                IsAutoGenerated = true,
                ParentScheduleId = Id
            };
        }

        /// <summary>
        /// Mark schedule as completed with test result.
        /// </summary>
        /// <param name="testResult">Completed INR test.</param>
        public void MarkAsCompleted(INRTest testResult)
        {
            Status = INRScheduleStatus.Completed;
            CompletedDate = testResult.TestDate.Date;
            CompletedTestId = testResult.Id;
            UpdatedAt = DateTime.UtcNow;

            // Auto-generate next schedule if recurring
            NextScheduledDate = GenerateNextSchedule()?.ScheduledDate;
        }

        /// <summary>
        /// Reschedule to a new date.
        /// </summary>
        /// <param name="newDate">New scheduled date.</param>
        /// <param name="reason">Reason for rescheduling.</param>
        public void Reschedule(DateTime newDate, string reason)
        {
            ScheduledDate = newDate;
            ModificationReason = reason;
            Status = INRScheduleStatus.Rescheduled;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancel the schedule.
        /// </summary>
        /// <param name="reason">Reason for cancellation.</param>
        public void Cancel(string reason)
        {
            Status = INRScheduleStatus.Cancelled;
            ModificationReason = reason;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Validate INR schedule for medical compliance.
        /// </summary>
        /// <returns>List of validation errors.</returns>
        public List<string> ValidateForMedicalCompliance()
        {
            var errors = new List<string>();

            if (ScheduledDate.Date < DateTime.UtcNow.Date.AddDays(-1))
                errors.Add("Scheduled date cannot be more than 1 day in the past");

            if (TargetINRMin.HasValue && TargetINRMax.HasValue)
            {
                if (TargetINRMin.Value >= TargetINRMax.Value)
                    errors.Add("Target INR minimum must be less than maximum");
            }

            if (IntervalDays <= 0 && Frequency != INRTestFrequency.OneTime)
                errors.Add("Interval days must be greater than 0 for recurring schedules");

            if (EndDate.HasValue && EndDate.Value <= ScheduledDate.Date)
                errors.Add("End date must be after scheduled date");

            if (ReminderDays < 0 || ReminderDays > 14)
                errors.Add("Reminder days must be between 0 and 14");

            // Medical safety checks
            if (IntervalDays > 90)
                errors.Add("WARNING: INR testing interval longer than 90 days may not be safe");

            if (Frequency == INRTestFrequency.Weekly && IntervalDays != 7)
                errors.Add("Weekly frequency should have 7-day interval");

            return errors;
        }

        /// <summary>
        /// Get schedule status description.
        /// </summary>
        /// <returns>Human-readable status description.</returns>
        public string GetStatusDescription()
        {
            return Status switch
            {
                INRScheduleStatus.Active when IsDue() => $"Due {(DaysUntilTest() == 0 ? "today" : $"{Math.Abs(DaysUntilTest())} days ago")}",
                INRScheduleStatus.Active when DaysUntilTest() > 0 => $"Due in {DaysUntilTest()} days",
                INRScheduleStatus.Active => "Scheduled",
                INRScheduleStatus.Completed => $"Completed on {CompletedDate:d}",
                INRScheduleStatus.Rescheduled => "Rescheduled",
                INRScheduleStatus.Cancelled => "Cancelled",
                INRScheduleStatus.Missed => "Missed",
                _ => Status.ToString()
            };
        }
    }

    /// <summary>
    /// INR test frequency options.
    /// </summary>
    public enum INRTestFrequency
    {
        /// <summary>
        /// One-time test only.
        /// </summary>
        OneTime = 0,

        /// <summary>
        /// Weekly testing.
        /// </summary>
        Weekly = 1,

        /// <summary>
        /// Every two weeks.
        /// </summary>
        BiWeekly = 2,

        /// <summary>
        /// Monthly testing.
        /// </summary>
        Monthly = 3,

        /// <summary>
        /// Every 6 weeks.
        /// </summary>
        SixWeeks = 4,

        /// <summary>
        /// Every 2 months.
        /// </summary>
        BiMonthly = 5,

        /// <summary>
        /// Quarterly (every 3 months).
        /// </summary>
        Quarterly = 6,

        /// <summary>
        /// Custom interval.
        /// </summary>
        Custom = 99
    }

    /// <summary>
    /// INR schedule status options.
    /// </summary>
    public enum INRScheduleStatus
    {
        /// <summary>
        /// Schedule is active and pending.
        /// </summary>
        Active = 0,

        /// <summary>
        /// Schedule was completed.
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Schedule was rescheduled.
        /// </summary>
        Rescheduled = 2,

        /// <summary>
        /// Schedule was cancelled.
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Schedule was missed (overdue).
        /// </summary>
        Missed = 4
    }
}