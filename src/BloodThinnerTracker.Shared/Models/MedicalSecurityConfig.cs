// BloodThinnerTracker.Shared - Medical Security Configuration Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    /// <summary>
    /// Medical application security configuration.
    /// </summary>
    public class MedicalSecurityConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether multi-factor authentication is required for medical data access.
        /// </summary>
        public bool RequireMfa { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum failed login attempts before account lockout.
        /// </summary>
        public int MaxFailedLoginAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the account lockout duration in minutes.
        /// </summary>
        public int LockoutDurationMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether password complexity is required for local accounts.
        /// </summary>
        public bool RequirePasswordComplexity { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum password length for local accounts.
        /// </summary>
        public int MinPasswordLength { get; set; } = 12;

        /// <summary>
        /// Gets or sets the session timeout in minutes for medical security.
        /// </summary>
        public int SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether device registration is required for medical data access.
        /// </summary>
        public bool RequireDeviceRegistration { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether audit logging is enabled for all authentication events.
        /// </summary>
        public bool EnableAuditLogging { get; set; } = true;
    }
}