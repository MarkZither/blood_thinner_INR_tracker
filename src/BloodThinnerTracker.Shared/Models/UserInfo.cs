// BloodThinnerTracker.Shared - User Information Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// User information for authentication response.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Gets or sets the unique user identifier.
        /// </summary>
        [Required]
        public Guid PublicId { get; set; }

        /// <summary>
        /// Gets or sets the user email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user role for medical data access.
        /// </summary>
        public string Role { get; set; } = "Patient";

        /// <summary>
        /// Gets or sets the authentication provider (Azure AD, Google, Local).
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's timezone for medication scheduling.
        /// </summary>
        public string TimeZone { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets the last login timestamp for security tracking.
        /// </summary>
        public DateTime? LastLogin { get; set; }
    }
}
