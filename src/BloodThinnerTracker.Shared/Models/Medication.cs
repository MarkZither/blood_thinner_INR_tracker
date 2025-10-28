// BloodThinnerTracker.Shared - Medication Entity for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Medication entity for blood thinner medication tracking.
    /// Represents a specific blood thinner medication prescribed to a user.
    /// 
    /// MEDICAL DISCLAIMER: This entity stores prescription medication information.
    /// Always verify medication details with healthcare providers.
    /// </summary>
    [Table("Medications")]
public class Medication : MedicalEntityBase
{
    /// <summary>
    /// Gets or sets the medication name (e.g., "Warfarin", "Heparin").
    /// </summary>
    [Required]
    [StringLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = string.Empty;        /// <summary>
        /// Gets or sets the generic medication name.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? GenericName { get; set; }

        /// <summary>
        /// Gets or sets the brand medication name.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? BrandName { get; set; }

        /// <summary>
        /// Gets or sets the medication type/category.
        /// </summary>
        [Required]
        public MedicationType Type { get; set; }

        /// <summary>
        /// Gets or sets the medication dosage amount.
        /// </summary>
        [Required]
        [Range(0.1, 1000.0, ErrorMessage = "Dosage must be between 0.1 and 1000")]
        [Column(TypeName = "decimal(10,3)")]
        public decimal Dosage { get; set; }

        /// <summary>
        /// Gets or sets the dosage unit (mg, mcg, units, etc.).
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string DosageUnit { get; set; } = "mg";

        /// <summary>
        /// Gets or sets the medication strength as a numeric value.
        /// </summary>
        [Range(0.01, 1000, ErrorMessage = "Strength must be between 0.01 and 1000")]
        [Column(TypeName = "decimal(10,3)")]
        public decimal? Strength { get; set; }

        /// <summary>
        /// Gets or sets the medication unit for strength.
        /// </summary>
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? Unit { get; set; }

        /// <summary>
        /// Gets or sets the medication form (tablet, capsule, liquid, injection).
        /// </summary>
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? Form { get; set; }

        /// <summary>
        /// Gets or sets the medication color for identification.
        /// </summary>
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets the medication shape for identification.
        /// </summary>
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? Shape { get; set; }

        /// <summary>
        /// Gets or sets the medication imprint/markings.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? Imprint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a blood thinner medication.
        /// </summary>
        public bool IsBloodThinner { get; set; } = true;

        /// <summary>
        /// Gets or sets contraindications for this medication.
        /// </summary>
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? Contraindications { get; set; }

        /// <summary>
        /// Gets or sets storage instructions for the medication.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? StorageInstructions { get; set; }

        /// <summary>
        /// Gets or sets the maximum daily dose allowed.
        /// </summary>
        [Column(TypeName = "decimal(10,3)")]
        public decimal MaxDailyDose { get; set; } = 0;

        /// <summary>
        /// Gets or sets the minimum hours required between doses.
        /// </summary>
        [Range(1, 168, ErrorMessage = "Minimum hours between doses must be between 1 and 168")]
        public int MinHoursBetweenDoses { get; set; } = 6;

        /// <summary>
        /// Gets or sets a value indicating whether this medication requires INR monitoring.
        /// </summary>
        public bool RequiresINRMonitoring { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum target INR value for this medication.
        /// </summary>
        [Range(0.5, 8.0, ErrorMessage = "INR target minimum must be between 0.5 and 8.0")]
        [Column(TypeName = "decimal(3,1)")]
        public decimal? INRTargetMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum target INR value for this medication.
        /// </summary>
        [Range(0.5, 8.0, ErrorMessage = "INR target maximum must be between 0.5 and 8.0")]
        [Column(TypeName = "decimal(3,1)")]
        public decimal? INRTargetMax { get; set; }

        /// <summary>
        /// Gets or sets the medication frequency (how often to take).
        /// </summary>
        [Required]
        public MedicationFrequency Frequency { get; set; }

        /// <summary>
        /// Gets or sets custom frequency description for irregular schedules.
        /// </summary>
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string? CustomFrequency { get; set; }

        /// <summary>
        /// Gets or sets the preferred time(s) to take medication.
        /// Stored as JSON array of time strings (e.g., ["08:00", "20:00"]).
        /// </summary>
        [Column(TypeName = "nvarchar(500)")]
        public string? ScheduledTimes { get; set; }

        /// <summary>
        /// Gets or sets the medication start date.
        /// </summary>
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the medication end date (if applicable).
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the medication is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the prescribing healthcare provider.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? PrescribedBy { get; set; }

        /// <summary>
        /// Gets or sets the prescription date.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? PrescriptionDate { get; set; }

        /// <summary>
        /// Gets or sets the pharmacy information.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? Pharmacy { get; set; }

        /// <summary>
        /// Gets or sets the prescription number.
        /// </summary>
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? PrescriptionNumber { get; set; }

        /// <summary>
        /// Gets or sets special instructions for taking the medication.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? Instructions { get; set; }

        /// <summary>
        /// Gets or sets food interaction warnings.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? FoodInteractions { get; set; }

        /// <summary>
        /// Gets or sets drug interaction warnings.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? DrugInteractions { get; set; }

        /// <summary>
        /// Gets or sets side effects to monitor.
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? SideEffects { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether reminders are enabled.
        /// </summary>
        public bool RemindersEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the reminder advance time in minutes.
        /// </summary>
        [Range(0, 1440, ErrorMessage = "Reminder time must be between 0 and 1440 minutes")]
        public int ReminderMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets additional notes about the medication.
        /// </summary>
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? Notes { get; set; }

        // Navigation properties

        /// <summary>
        /// Gets or sets the user who owns this medication.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the medication logs for this medication.
        /// </summary>
        public virtual ICollection<MedicationLog> MedicationLogs { get; set; } = new List<MedicationLog>();

        /// <summary>
        /// Check if medication is currently valid (within date range).
        /// </summary>
        /// <returns>True if medication is currently valid.</returns>
        public bool IsCurrentlyValid()
        {
            var now = DateTime.UtcNow.Date;
            return IsActive && 
                   StartDate.Date <= now && 
                   (!EndDate.HasValue || EndDate.Value.Date >= now);
        }

        /// <summary>
        /// Get next scheduled dose time.
        /// </summary>
        /// <param name="userTimeZone">User's timezone for calculation.</param>
        /// <returns>Next scheduled dose time in user's timezone.</returns>
        public DateTime? GetNextScheduledDose(string userTimeZone = "UTC")
        {
            if (!IsCurrentlyValid() || string.IsNullOrWhiteSpace(ScheduledTimes))
                return null;

            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZone);
                var userNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
                var today = userNow.Date;

                // Parse scheduled times from JSON
                var times = System.Text.Json.JsonSerializer.Deserialize<string[]>(ScheduledTimes);
                if (times == null || times.Length == 0)
                    return null;

                foreach (var timeStr in times.OrderBy(t => t))
                {
                    if (TimeSpan.TryParse(timeStr, out var timeSpan))
                    {
                        var scheduledTime = today.Add(timeSpan);
                        if (scheduledTime > userNow)
                        {
                            return TimeZoneInfo.ConvertTimeToUtc(scheduledTime, timeZone);
                        }
                    }
                }

                // If no time today, get first time tomorrow
                if (times.Length > 0 && TimeSpan.TryParse(times[0], out var firstTime))
                {
                    var tomorrowTime = today.AddDays(1).Add(firstTime);
                    return TimeZoneInfo.ConvertTimeToUtc(tomorrowTime, timeZone);
                }
            }
            catch (Exception)
            {
                // Log error in real implementation
                return null;
            }

            return null;
        }

        /// <summary>
        /// Validate medication for medical safety.
        /// </summary>
        /// <returns>List of validation errors.</returns>
        public List<string> ValidateForMedicalSafety()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Medication name is required");

            if (Dosage <= 0)
                errors.Add("Medication dosage must be greater than 0");

            if (string.IsNullOrWhiteSpace(DosageUnit))
                errors.Add("Dosage unit is required");

            if (StartDate > DateTime.UtcNow.Date)
                errors.Add("Start date cannot be in the future");

            if (EndDate.HasValue && EndDate.Value < StartDate)
                errors.Add("End date cannot be before start date");

            if (Type == MedicationType.Warfarin && Dosage > 20)
                errors.Add("Warfarin dosage above 20mg requires special attention");

            if (ReminderMinutes < 0 || ReminderMinutes > 1440)
                errors.Add("Reminder time must be between 0 and 1440 minutes");

            return errors;
        }

        /// <summary>
        /// Get medication display name with dosage.
        /// </summary>
        /// <returns>Formatted medication name with dosage.</returns>
        public string GetDisplayName()
        {
            return $"{Name} {Dosage}{DosageUnit}";
        }
    }

    /// <summary>
    /// Types of blood thinner medications.
    /// </summary>
    public enum MedicationType
    {
        /// <summary>
        /// Warfarin (Coumadin) - requires INR monitoring.
        /// </summary>
        Warfarin = 0,

        /// <summary>
        /// Direct Oral Anticoagulants (DOACs).
        /// </summary>
        DOAC = 1,

        /// <summary>
        /// Heparin - typically hospital/injection use.
        /// </summary>
        Heparin = 2,

        /// <summary>
        /// Low Molecular Weight Heparin.
        /// </summary>
        LMWH = 3,

        /// <summary>
        /// Antiplatelet medications (aspirin, clopidogrel).
        /// </summary>
        Antiplatelet = 4,

        /// <summary>
        /// Other blood thinner types.
        /// </summary>
        Other = 99
    }

    /// <summary>
    /// Medication frequency options.
    /// </summary>
    public enum MedicationFrequency
    {
        /// <summary>
        /// Once daily.
        /// </summary>
        OnceDaily = 0,

        /// <summary>
        /// Twice daily (every 12 hours).
        /// </summary>
        TwiceDaily = 1,

        /// <summary>
        /// Three times daily (every 8 hours).
        /// </summary>
        ThreeTimesDaily = 2,

        /// <summary>
        /// Four times daily (every 6 hours).
        /// </summary>
        FourTimesDaily = 3,

        /// <summary>
        /// Every other day.
        /// </summary>
        EveryOtherDay = 4,

        /// <summary>
        /// Weekly.
        /// </summary>
        Weekly = 5,

        /// <summary>
        /// As needed (PRN).
        /// </summary>
        AsNeeded = 6,

        /// <summary>
        /// Custom schedule (see CustomFrequency field).
        /// </summary>
        Custom = 99
    }
}