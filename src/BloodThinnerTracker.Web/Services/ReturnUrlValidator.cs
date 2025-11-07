using System.Net;
using System.Web;

namespace BloodThinnerTracker.Web.Services;

public static class ReturnUrlValidator
{
    // Validate the incoming raw returnUrl query parameter value.
    // Returns normalized path (starts with '/') when valid.
    public static ReturnUrlValidationResult Validate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new ReturnUrlValidationResult(false, null, "missing");

        // Decode once - HttpUtility.UrlDecode is safe and doesn't throw for malformed input
        var decoded = HttpUtility.UrlDecode(raw);

        if (string.IsNullOrWhiteSpace(decoded))
            return new ReturnUrlValidationResult(false, null, "malformed");

        // Reject protocol-relative leading '//' which may appear after decode
        if (decoded.StartsWith("//"))
            return new ReturnUrlValidationResult(false, null, "protocol-relative");

        // Reject javascript: or data: schemes or other absolute URIs
        var colonIndex = decoded.IndexOf(':');
        if (colonIndex > 0)
        {
            // If there's a colon before any slash, treat as scheme
            var firstSlash = decoded.IndexOf('/');
            if (firstSlash == -1 || colonIndex < firstSlash)
                return new ReturnUrlValidationResult(false, null, "invalid-scheme");
        }

        // Check for suspicious double-encoding that would yield leading '/'
        // e.g., %252F%2Fevil -> decodes to %2F%2Fevil -> after another decode starts with //
        if (decoded.IndexOf('%') >= 0)
        {
            // If second decode yields leading '/' or '//' or scheme, reject
            // HttpUtility.UrlDecode is safe and doesn't throw for malformed input
            var second = HttpUtility.UrlDecode(decoded);
            if (second.StartsWith("//"))
                return new ReturnUrlValidationResult(false, null, "double-encoded");
            var idx = second.IndexOf(':');
            if (idx > 0)
            {
                var fs = second.IndexOf('/');
                if (fs == -1 || idx < fs)
                    return new ReturnUrlValidationResult(false, null, "double-encoded");
            }
        }

        // Must start with single '/'
        if (!decoded.StartsWith('/'))
            return new ReturnUrlValidationResult(false, null, "not-relative");

        // Normalize: remove duplicate slashes at start
        var normalized = '/' + decoded.TrimStart('/');

        // Optionally enforce a max length
        if (normalized.Length > 2000)
            return new ReturnUrlValidationResult(false, null, "too-long");

        return new ReturnUrlValidationResult(true, normalized, null);
    }
}
