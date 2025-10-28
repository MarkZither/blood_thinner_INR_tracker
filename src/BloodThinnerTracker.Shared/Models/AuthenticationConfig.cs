// BloodThinnerTracker.Shared - Authentication Configuration Models
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication
{
    /// <summary>
    /// Authentication configuration for medical application.
    /// </summary>
    public class AuthenticationConfig
    {
        /// <summary>
        /// Gets or sets the JWT configuration settings.
        /// </summary>
        public JwtConfig Jwt { get; set; } = new ();

        /// <summary>
        /// Gets or sets the Azure AD configuration settings.
        /// </summary>
        public AzureAdConfig AzureAd { get; set; } = new ();

        /// <summary>
        /// Gets or sets the Google OAuth configuration settings.
        /// </summary>
        public GoogleConfig Google { get; set; } = new ();

        /// <summary>
        /// Gets or sets the medical application security settings.
        /// </summary>
        public MedicalSecurityConfig MedicalSecurity { get; set; } = new ();
    }
}