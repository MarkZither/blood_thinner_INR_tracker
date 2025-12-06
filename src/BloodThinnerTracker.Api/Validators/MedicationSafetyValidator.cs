using BloodThinnerTracker.Api.Models.V1;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Validators;

/// <summary>
/// Encapsulates medication safety validation rules for CreateMedicationRequest.
/// Returns the project `ValidationResult` model so callers/tests don't need to
/// depend on FluentValidation types.
/// </summary>
public static class MedicationSafetyValidator
{
    public static ValidationResult Validate(CreateMedicationRequest request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            return new ValidationResult { IsValid = false, Errors = new List<string> { "request is null" } };
        }

        // Basic rules
        if (request.Dosage <= 0)
        {
            errors.Add("dosage must be greater than 0");
        }

        // Blood thinner timing rule
        if (request.IsBloodThinner)
        {
            if (request.MinHoursBetweenDoses.HasValue && request.MinHoursBetweenDoses.Value < 12)
            {
                errors.Add("12 hours between doses is required for blood thinners");
            }
        }

        // Warfarin-specific rules
        if (request.Type == MedicationType.VitKAntagonist)
        {
            if (request.RequiresINRMonitoring != true)
            {
                errors.Add("Warfarin requires INR monitoring");
            }

            if (request.RequiresINRMonitoring == true)
            {
                if (!request.INRTargetMin.HasValue || !request.INRTargetMax.HasValue)
                {
                    errors.Add("INR target range is required (recommended 2.0-3.0)");
                }
                else
                {
                    var min = request.INRTargetMin.Value;
                    var max = request.INRTargetMax.Value;
                    if (min < 0.5m || max > 8.0m)
                    {
                        errors.Add("INR target range must be between 0.5 and 8.0");
                    }
                    if (min >= max)
                    {
                        errors.Add("INR minimum must be less than maximum");
                    }
                }
            }

            if (request.MaxDailyDose.HasValue && request.MaxDailyDose.Value > 20m)
            {
                errors.Add("Warfarin dosing above 20mg requires specialist oversight");
            }
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
