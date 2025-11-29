using System.Security.Claims;

namespace BloodThinnerTracker.Api.Services.Authentication
{
    /// <summary>
    /// Helper to resolve an external user id from ID token claims.
    /// Centralises precedence logic so all callers behave consistently.
    /// </summary>
    public static class IdTokenClaimResolver
    {
        /// <summary>
        /// Resolve the external user id for a claims principal.
        /// Precedence: `oid` (or objectidentifier schema) -> `sub` -> null
        /// </summary>
        public static string? ResolveExternalUserId(ClaimsPrincipal? principal)
        {
            if (principal == null) return null;

            // Prefer the short 'oid' claim used by Azure AD
            var oid = principal.FindFirst("oid")?.Value;
            if (!string.IsNullOrEmpty(oid)) return oid;

            // Some tokens use the full schema URI
            var objectId = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (!string.IsNullOrEmpty(objectId)) return objectId;

            // Fallback to subject claim
            var sub = principal.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub)) return sub;

            return null;
        }
    }
}
