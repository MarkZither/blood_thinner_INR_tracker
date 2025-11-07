namespace BloodThinnerTracker.Web.Services;

public record ReturnUrlValidationResult(bool IsValid, string? Normalized, string? ValidationResultCode);
