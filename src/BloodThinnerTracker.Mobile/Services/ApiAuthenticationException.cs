using System;
using System.Net;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Thrown when the remote API indicates an authentication problem (401 Unauthorized).
    /// Callers can catch this to trigger a local re-authentication / navigation to login.
    /// </summary>
    public class ApiAuthenticationException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? ResponseBody { get; }

        public ApiAuthenticationException(HttpStatusCode statusCode, string? responseBody = null)
            : base(statusCode == HttpStatusCode.Unauthorized ? "API authentication required (401)" : $"API returned {statusCode}")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
