// BloodThinnerTracker.Shared - User Entity for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// User entity for blood thinner medication tracking application.
    /// Represents a patient or healthcare provider using the system.
    ///
    /// MEDICAL DISCLAIMER: This entity stores personal health information.
    /// Ensure compliance with HIPAA and other healthcare regulations.
    ///
    /// SECURITY NOTE: User is the root tenant entity and does NOT inherit from MedicalEntityBase.
    /// User defines the tenant boundary - medical data belongs TO users, not the other way around.
    /// </summary>
    [Table("Users")]
    public class User
    {
        /// <summary>
        /// Gets or sets the internal database identifier for this entity.
        /// ⚠️ SECURITY: Internal use only - NEVER expose this in APIs!
        /// Use PublicId for all external references.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the public-facing identifier for API consumers.
        /// ⚠️ SECURITY: Always use this in API responses instead of Id.
        /// Non-sequential GUID prevents IDOR attacks and enumeration attacks.
        /// </summary>
        [Required]
        public Guid PublicId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets when this record was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when this record was last updated.
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets a value indicating whether this user account is soft deleted for data retention compliance.
        /// User data cannot be permanently deleted immediately due to legal requirements (GDPR right to be forgotten, etc.).
        /// </summary>
        [Required]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Gets or sets when this user account was soft deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the user's email address (used for authentication).
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Column(TypeName = "varchar(256)")]
    public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's display name.
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the user's date of birth (important for medication dosing).
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Gets or sets the user's phone number for emergency contact.
        /// </summary>
        [Phone]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the user's timezone for accurate medication scheduling.
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string TimeZone { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets the user's role in the medical system.
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public UserRole Role { get; set; } = UserRole.Patient;

        /// <summary>
        /// Gets or sets the authentication provider (Local, AzureAD, Google).
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string AuthProvider { get; set; } = "Local";

        /// <summary>
        /// Gets or sets the external provider user ID.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? ExternalUserId { get; set; }

        /// <summary>
        /// Gets or sets the last login timestamp for security tracking.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user account is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the user's email is verified.
        /// </summary>
        public bool EmailVerified { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether two-factor authentication is enabled.
        /// </summary>
        public bool TwoFactorEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the user's emergency contact name.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? EmergencyContactName { get; set; }

        /// <summary>
        /// Gets or sets the user's emergency contact phone number.
        /// </summary>
        [Phone]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? EmergencyContactPhone { get; set; }

        /// <summary>
        /// Gets or sets the user's healthcare provider name.
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? HealthcareProvider { get; set; }

        /// <summary>
        /// Gets or sets the healthcare provider's phone number.
        /// </summary>
        [Phone]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? HealthcareProviderPhone { get; set; }

        /// <summary>
        /// Gets or sets medical notes or special instructions.
        /// </summary>
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? MedicalNotes { get; set; }

        /// <summary>
        /// Gets or sets user preferences as JSON.
        /// </summary>
        [StringLength(4000)]
        [Column(TypeName = "nvarchar(4000)")]
        public string? Preferences { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether email notifications are enabled.
        /// </summary>
        public bool IsEmailNotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether SMS notifications are enabled.
        /// </summary>
        public bool IsSmsNotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether push notifications are enabled.
        /// </summary>
        public bool IsPushNotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the preferred language for the user interface.
        /// </summary>
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string PreferredLanguage { get; set; } = "en";

        /// <summary>
        /// Gets or sets how many minutes in advance to send medication reminders.
        /// </summary>
        [Range(0, 1440, ErrorMessage = "Reminder advance time must be between 0 and 1440 minutes")]
        public int ReminderAdvanceMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets when the user profile was completed.
        /// </summary>
        public DateTime? ProfileCompletedAt { get; set; }

        // Navigation properties for medical data relationships

        /// <summary>
        /// Gets or sets the medications associated with this user.
        /// </summary>
        public virtual ICollection<Medication> Medications { get; set; } = new List<Medication>();

        /// <summary>
        /// Gets or sets the medication logs associated with this user.
        /// </summary>
        public virtual ICollection<MedicationLog> MedicationLogs { get; set; } = new List<MedicationLog>();

        /// <summary>
        /// Gets or sets the INR tests associated with this user.
        /// </summary>
        public virtual ICollection<INRTest> INRTests { get; set; } = new List<INRTest>();

        /// <summary>
        /// Gets or sets the INR schedules associated with this user.
        /// </summary>
        public virtual ICollection<INRSchedule> INRSchedules { get; set; } = new List<INRSchedule>();

        /// <summary>
        /// Calculate user's age from date of birth.
        /// </summary>
        /// <returns>Age in years, or null if date of birth is not set.</returns>
        public int? GetAge()
        {
            if (!DateOfBirth.HasValue)
                return null;

            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Value.Year;

            if (DateOfBirth.Value.Date > today.AddYears(-age))
                age--;

            return age;
        }

        /// <summary>
        /// Get user's full name.
        /// </summary>
        /// <returns>Combined first and last name, or display name if parts not available.</returns>
        public string GetFullName()
        {
            if (!string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName))
                return $"{FirstName} {LastName}";

            return Name;
        }

        /// <summary>
        /// Check if user is a healthcare provider.
        /// </summary>
        /// <returns>True if user has healthcare provider role.</returns>
        public bool IsHealthcareProvider()
        {
            return Role == UserRole.HealthcareProvider || Role == UserRole.Admin;
        }

        /// <summary>
        /// Validate user entity for medical compliance.
        /// </summary>
        /// <returns>List of validation errors.</returns>
        public List<string> ValidateForMedicalCompliance()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Email))
                errors.Add("Email is required for medical data access");

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name is required for medical identification");

            if (DateOfBirth.HasValue && DateOfBirth.Value > DateTime.Today)
                errors.Add("Date of birth cannot be in the future");

            if (DateOfBirth.HasValue && GetAge() > 150)
                errors.Add("Invalid date of birth - age cannot exceed 150 years");

            if (!string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber.Length < 10)
                errors.Add("Phone number must be at least 10 digits for emergency contact");

            return errors;
        }
    }

    /// <summary>
    /// User roles in the medical system.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Regular patient using the system for medication tracking.
        /// </summary>
        Patient = 0,

        /// <summary>
        /// Healthcare provider with extended access to patient data.
        /// </summary>
        HealthcareProvider = 1,

        /// <summary>
        /// System administrator with full access.
        /// </summary>
        Admin = 2,

        /// <summary>
        /// Caregiver with limited access to assist patients.
        /// </summary>
        Caregiver = 3
    }
}
