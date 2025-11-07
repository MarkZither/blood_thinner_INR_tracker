namespace BloodThinnerTracker.Api.Validators;

using BloodThinnerTracker.Shared.Models;
using FluentValidation;

/// <summary>
/// Validator for CreateDosagePatternRequest ensuring pattern data integrity and safety.
/// </summary>
/// <remarks>
/// ⚠️ MEDICAL DATA SAFETY: This validator enforces critical constraints for medication dosage patterns.
/// Validation rules prevent unsafe dosages, invalid date ranges, and data integrity violations.
/// </remarks>
public class CreateDosagePatternRequestValidator : AbstractValidator<CreateDosagePatternRequest>
{
    public CreateDosagePatternRequestValidator()
    {
        // Pattern sequence validation
        RuleFor(x => x.PatternSequence)
            .NotNull()
            .WithMessage("Pattern sequence is required")
            .NotEmpty()
            .WithMessage("Pattern must contain at least one dosage value");

        RuleFor(x => x.PatternSequence)
            .Must(seq => seq != null && seq.Count >= 1)
            .WithMessage("Pattern must have at least 1 dosage")
            .Must(seq => seq != null && seq.Count <= 365)
            .WithMessage("Pattern cannot exceed 365 dosages");

        // Individual dosage value validation
        RuleForEach(x => x.PatternSequence)
            .InclusiveBetween(0.1m, 1000.0m)
            .WithMessage("Each dosage must be between 0.1 and 1000.0 mg");

        // Start date validation
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required")
            .Must(date => date >= DateTime.UtcNow.Date.AddYears(-1))
            .WithMessage("Start date cannot be more than 1 year in the past");

        // Backdating warning (>7 days in past) - FR-011
        // NOTE: This is informational only. API accepts any valid past date.
        // UI should show confirmation dialog for >7 days backdating (T051).
        RuleFor(x => x.StartDate)
            .Must(date => date >= DateTime.UtcNow.Date.AddDays(-7))
            .WithMessage("Pattern start date is more than 7 days in the past. This will affect historical medication logs. Please confirm this is intentional.")
            .WithSeverity(FluentValidation.Severity.Warning);

        // End date validation
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be on or after the start date");

        // Notes length validation
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters");

        // Single-value pattern warning (informational)
        RuleFor(x => x.PatternSequence)
            .Must(seq => seq == null || seq.Count != 1)
            .WithMessage("Pattern contains only one dosage value. Consider using a fixed daily dose instead of a pattern.")
            .WithSeverity(FluentValidation.Severity.Warning);

        // Long pattern warning (informational)
        RuleFor(x => x.PatternSequence)
            .Must(seq => seq == null || seq.Count <= 20)
            .WithMessage("Pattern is unusually long ({PropertyValue} days). Please verify this is correct.");
    }

    /// <summary>
    /// Validates medication-specific dosage constraints (e.g., Warfarin max 20mg).
    /// This method should be called after the standard validation in the controller.
    /// </summary>
    /// <param name="request">The pattern request to validate</param>
    /// <param name="medicationName">Name of the medication for specific rules</param>
    /// <param name="medicationType">Type/category of the medication</param>
    /// <returns>Validation result with medication-specific errors</returns>
    public static FluentValidation.Results.ValidationResult ValidateMedicationSpecificRules(
        CreateDosagePatternRequest request,
        string medicationName,
        string? medicationType = null)
    {
        var errors = new List<FluentValidation.Results.ValidationFailure>();

        // Warfarin-specific validation
        if (medicationName?.Contains("warfarin", StringComparison.OrdinalIgnoreCase) == true ||
            medicationType?.Contains("warfarin", StringComparison.OrdinalIgnoreCase) == true)
        {
            var maxDosage = request.PatternSequence.Max();
            if (maxDosage > 20.0m)
            {
                errors.Add(new FluentValidation.Results.ValidationFailure(
                    nameof(request.PatternSequence),
                    $"Warfarin dosage should not exceed 20mg. Pattern contains {maxDosage}mg. Please verify with healthcare provider."
                ));
            }
        }

        return new FluentValidation.Results.ValidationResult(errors);
    }
}
