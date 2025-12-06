using BloodThinnerTracker.Api.Models.V1;
using BloodThinnerTracker.Shared.Models;
using FluentValidation;

namespace BloodThinnerTracker.Api.Validators;

/// <summary>
/// FluentValidation rules for `CreateMedicationRequest`.
/// Ensures medication safety rules (timing, INR monitoring, dosage limits).
/// </summary>
public class CreateMedicationRequestValidator : AbstractValidator<CreateMedicationRequest>
{
    public CreateMedicationRequestValidator()
    {
        RuleFor(x => x.Dosage)
            .GreaterThan(0).WithMessage("dosage must be greater than 0");

        RuleFor(x => x.MinHoursBetweenDoses)
            .GreaterThanOrEqualTo(12)
            .When(x => x.IsBloodThinner)
            .WithMessage("12 hours between doses is required for blood thinners");

        // Warfarin (Vitamin K antagonist) specific rules
        RuleFor(x => x.RequiresINRMonitoring)
            .Equal(true)
            .When(x => x.Type == MedicationType.VitKAntagonist)
            .WithMessage("Warfarin requires INR monitoring");

        // INR target presence when monitoring is enabled
        RuleFor(x => x.INRTargetMin)
            .NotNull()
            .When(x => x.RequiresINRMonitoring == true && x.Type == MedicationType.VitKAntagonist)
            .WithMessage("INR target range is required (recommended 2.0-3.0)");

        RuleFor(x => x.INRTargetMax)
            .NotNull()
            .When(x => x.RequiresINRMonitoring == true && x.Type == MedicationType.VitKAntagonist)
            .WithMessage("INR target range is required (recommended 2.0-3.0)");

        RuleFor(x => x)
            .Custom((req, ctx) =>
            {
                if (req.RequiresINRMonitoring == true && req.INRTargetMin.HasValue && req.INRTargetMax.HasValue)
                {
                    var min = req.INRTargetMin.Value;
                    var max = req.INRTargetMax.Value;
                    if (min < 0.5m || max > 8.0m)
                    {
                        ctx.AddFailure("INR target range must be between 0.5 and 8.0");
                    }
                    if (min >= max)
                    {
                        ctx.AddFailure("INR minimum must be less than maximum");
                    }
                }
            });

        RuleFor(x => x.MaxDailyDose)
            .LessThanOrEqualTo(20m)
            .When(x => x.Type == MedicationType.VitKAntagonist && x.MaxDailyDose.HasValue)
            .WithMessage("Warfarin dosing above 20mg requires specialist oversight");
    }
}
