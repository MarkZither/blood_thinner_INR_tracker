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
        /// Gets or sets the dosage patterns for this medication.
        /// Multiple patterns enable temporal tracking of pattern changes over time.
        /// </summary>
        /// <remarks>
        /// ⚠️ PATTERN-BASED DOSING: Collection of variable-dosage schedules with temporal validity.
        /// - Empty collection = medication uses single fixed Dosage property (backward compatible)
        /// - One or more patterns = medication uses pattern-based dosing (Dosage property becomes fallback)
        /// - Patterns are ordered by StartDate for historical tracking
        /// - Only one pattern should have EndDate = NULL (active pattern)
        /// </remarks>
        public virtual ICollection<MedicationDosagePattern> DosagePatterns { get; set; } 
            = new List<MedicationDosagePattern>();

        // Computed Properties for Pattern Support

        /// <summary>
        /// Gets the currently active dosage pattern (where EndDate is NULL).
        /// Returns null if no patterns exist or all patterns are historical.
        /// </summary>
        /// <remarks>
        /// ⚠️ ACTIVE PATTERN LOOKUP: Most recent pattern with NULL EndDate.
        /// - Multiple active patterns should be prevented by validation
        /// - If multiple exist, returns most recent by StartDate
        /// - Used for "Edit Pattern" and "View Current Schedule" features
        /// </remarks>
        [NotMapped]
        public MedicationDosagePattern? ActivePattern => DosagePatterns?
            .Where(p => p.EndDate == null)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefault();

        /// <summary>
        /// Gets a value indicating whether this medication uses pattern-based dosing.
        /// True if any patterns exist, false if using single fixed dosage.
        /// </summary>
        /// <remarks>
        /// Used for UI conditional rendering:
        /// - If true: Show pattern display, schedule view, variance tracking
        /// - If false: Show single dosage field (legacy mode)
        /// </remarks>
        [NotMapped]
        public bool HasPatternSchedule => DosagePatterns?.Any() ?? false;

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
        /// Gets the expected dosage for a specific date, considering active patterns.
        /// Falls back to single Dosage property if no patterns exist (backward compatibility).
        /// </summary>
        /// <param name="targetDate">Date to calculate dosage for.</param>
        /// <returns>Expected dosage, or null if no pattern/dosage is defined for that date.</returns>
        /// <remarks>
        /// ⚠️ PATTERN-AWARE DOSAGE CALCULATION: Core method for all dosage lookups.
        /// 
        /// Algorithm:
        /// 1. Find pattern active on targetDate (StartDate &lt;= targetDate &lt;= EndDate)
        /// 2. If pattern found, calculate dosage using pattern's GetDosageForDate()
        /// 3. If no pattern, fallback to single Dosage property (legacy mode)
        /// 4. Respect medication's StartDate/EndDate/IsActive constraints
        /// 
        /// Used by:
        /// - LogDose page (auto-populate expected dosage)
        /// - MedicationLog variance calculations
        /// - Schedule view (generate future dosages)
        /// - Historical log corrections
        /// 
        /// Example: Medication with pattern [4.0, 4.0, 3.0] starting 2025-11-01
        /// - GetExpectedDosageForDate(2025-11-01) → 4.0
        /// - GetExpectedDosageForDate(2025-11-03) → 3.0
        /// - GetExpectedDosageForDate(2025-11-04) → 4.0 (pattern repeats)
        /// </remarks>
        public decimal? GetExpectedDosageForDate(DateTime targetDate)
        {
            // Find the pattern that was active on the target date
            var activePattern = GetPatternForDate(targetDate);

            if (activePattern != null)
            {
                return activePattern.GetDosageForDate(targetDate);
            }

            // Fallback to single fixed dosage (backward compatibility)
            if (StartDate.Date <= targetDate.Date && 
                (!EndDate.HasValue || EndDate.Value.Date >= targetDate.Date) &&
                IsActive)
            {
                return Dosage;
            }

            return null;
        }

        /// <summary>
        /// Gets the pattern that was active on a specific date (historical query).
        /// Enables accurate historical dosage calculations for past medication logs.
        /// </summary>
        /// <param name="targetDate">Date to find active pattern for.</param>
        /// <returns>The pattern active on that date, or null if none found.</returns>
        /// <remarks>
        /// ⚠️ TEMPORAL QUERY: Critical for historical accuracy per FR-013.
        /// 
        /// Returns the pattern where:
        /// - StartDate &lt;= targetDate &lt;= EndDate (or EndDate is NULL)
        /// - If multiple patterns match (shouldn't happen), returns most recent by StartDate
        /// 
        /// Used by:
        /// - GetExpectedDosageForDate() for dosage calculation
        /// - MedicationLog corrections (recalculate ExpectedDosage when viewing past logs)
        /// - Pattern history display
        /// - Variance reports (compare actual vs expected using historical patterns)
        /// </remarks>
        public MedicationDosagePattern? GetPatternForDate(DateTime targetDate)
        {
            return DosagePatterns?
                .Where(p => p.StartDate.Date <= targetDate.Date && 
                           (p.EndDate == null || p.EndDate.Value.Date >= targetDate.Date))
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefault();
        }

        /// <summary>
        /// Generates a future dosage schedule for display in Schedule view.
        /// </summary>
        /// <param name="startDate">Starting date for schedule (typically today).</param>
        /// <param name="days">Number of days to generate (typically 7-28).</param>
        /// <returns>List of date/dosage/pattern-day tuples for UI rendering.</returns>
        /// <remarks>
        /// ⚠️ SCHEDULE GENERATION: Used by Schedule page (User Story 4).
        /// 
        /// For each date in range:
        /// 1. Calculate expected dosage using GetExpectedDosageForDate()
        /// 2. Identify which pattern is active (if any)
        /// 3. Calculate pattern day number (1-based) and cycle length
        /// 4. Mark pattern change dates (new pattern StartDate)
        /// 
        /// Returns entries with:
        /// - Date: The scheduled date
        /// - Dosage: Expected dosage amount
        /// - PatternDay: Day number in pattern (e.g., "Day 2 of 6")
        /// - IsPatternChange: True if this date starts a new pattern
        /// 
        /// UI can use this to:
        /// - Display daily dosages in calendar/list view
        /// - Highlight pattern changes
        /// - Show "Day X of Y" indicators
        /// - Enable "Copy Schedule" feature
        /// </remarks>
        public List<DosageScheduleEntry> GetFutureSchedule(DateTime startDate, int days)
        {
            var schedule = new List<DosageScheduleEntry>();

            for (int i = 0; i < days; i++)
            {
                var date = startDate.Date.AddDays(i);
                var dosage = GetExpectedDosageForDate(date);
                var pattern = GetPatternForDate(date);

                if (dosage.HasValue)
                {
                    int? patternDay = null;
                    if (pattern != null && pattern.PatternLength > 0)
                    {
                        int daysSinceStart = (date - pattern.StartDate.Date).Days;
                        patternDay = (daysSinceStart % pattern.PatternLength) + 1;
                    }

                    schedule.Add(new DosageScheduleEntry
                    {
                        Date = date,
                        Dosage = dosage.Value,
                        DosageUnit = DosageUnit,
                        PatternDay = patternDay,
                        PatternLength = pattern?.PatternLength,
                        IsPatternChange = i > 0 && pattern?.StartDate.Date == date
                    });
                }
            }

            return schedule;
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

            if (Type == MedicationType.VitKAntagonist && Dosage > 20)
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
        /// Acenocoumarol (Sintrom)/Warfarin (Coumadin) - requires INR monitoring.
        /// </summary>
        VitKAntagonist = 0,

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

    /// <summary>
    /// Represents a single day in a medication dosage schedule.
    /// Used for displaying future/past dosage schedules in UI.
    /// </summary>
    /// <remarks>
    /// ⚠️ DTO FOR SCHEDULE DISPLAY: Lightweight object for schedule view rendering.
    /// 
    /// Used by:
    /// - Schedule page (User Story 4)
    /// - Medication details schedule preview
    /// - Pattern comparison views
    /// 
    /// Example entry:
    /// - Date: 2025-11-04
    /// - Dosage: 4.0
    /// - DosageUnit: "mg"
    /// - PatternDay: 2
    /// - PatternLength: 6
    /// - IsPatternChange: false
    /// - DisplayText: "4mg (Day 2/6)"
    /// </remarks>
    public class DosageScheduleEntry
    {
        /// <summary>
        /// Gets or sets the date for this schedule entry.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the dosage amount for this date.
        /// </summary>
        public decimal Dosage { get; set; }

        /// <summary>
        /// Gets or sets the dosage unit (e.g., "mg", "mcg").
        /// </summary>
        public string DosageUnit { get; set; } = "mg";

        /// <summary>
        /// Gets or sets the day number within the pattern cycle (1-based).
        /// Null if medication doesn't use patterns.
        /// </summary>
        public int? PatternDay { get; set; }

        /// <summary>
        /// Gets or sets the total length of the pattern cycle.
        /// Null if medication doesn't use patterns.
        /// </summary>
        public int? PatternLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this date starts a new pattern.
        /// Used to highlight pattern changes in UI.
        /// </summary>
        public bool IsPatternChange { get; set; }

        /// <summary>
        /// Gets a formatted display string for UI rendering.
        /// Example: "4mg (Day 2/6)" or "4mg" if no pattern.
        /// </summary>
        public string DisplayText => PatternDay.HasValue 
            ? $"{Dosage:0.##}{DosageUnit} (Day {PatternDay}/{PatternLength})"
            : $"{Dosage:0.##}{DosageUnit}";
    }
}
