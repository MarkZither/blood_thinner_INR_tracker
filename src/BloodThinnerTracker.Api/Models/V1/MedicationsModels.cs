using System.ComponentModel.DataAnnotations;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Models.V1;

public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public sealed class MedicationResponse
{
    public Guid PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? GenericName { get; set; }
    public decimal Dosage { get; set; }
    public string DosageUnit { get; set; } = string.Empty;
    public MedicationType Type { get; set; }
    public MedicationFrequency Frequency { get; set; }
    public string? CustomFrequency { get; set; }
    public string? ScheduledTimes { get; set; }
    public string? Form { get; set; }
    public string? Color { get; set; }
    public string? Shape { get; set; }
    public string? Imprint { get; set; }
    public bool IsBloodThinner { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PrescribedBy { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    public string? Pharmacy { get; set; }
    public string? PrescriptionNumber { get; set; }
    public string? Instructions { get; set; }
    public string? SideEffects { get; set; }
    public string? FoodInteractions { get; set; }
    public string? DrugInteractions { get; set; }
    public string? Contraindications { get; set; }
    public string? StorageInstructions { get; set; }
    public bool RemindersEnabled { get; set; }
    public int ReminderMinutes { get; set; }
    public string? Notes { get; set; }
    public decimal MaxDailyDose { get; set; }
    public int MinHoursBetweenDoses { get; set; }
    public bool RequiresINRMonitoring { get; set; }
    public decimal? INRTargetMin { get; set; }
    public decimal? INRTargetMax { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateMedicationRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? BrandName { get; set; }

    [StringLength(200)]
    public string? GenericName { get; set; }

    [Required]
    public MedicationType Type { get; set; } = MedicationType.Other;

    [Required]
    [Range(0.01, 1000)]
    public decimal Dosage { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string DosageUnit { get; set; } = string.Empty;

    [Required]
    public MedicationFrequency Frequency { get; set; } = MedicationFrequency.OnceDaily;

    [StringLength(500)]
    public string? ScheduledTimes { get; set; }

    [StringLength(50)]
    public string? Form { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [StringLength(50)]
    public string? Shape { get; set; }

    [StringLength(100)]
    public string? Imprint { get; set; }

    public bool IsBloodThinner { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(200)]
    public string? PrescribedBy { get; set; }

    [StringLength(200)]
    public string? Pharmacy { get; set; }

    [StringLength(1000)]
    public string? Instructions { get; set; }

    [StringLength(2000)]
    public string? SideEffects { get; set; }

    [StringLength(2000)]
    public string? Contraindications { get; set; }

    [StringLength(1000)]
    public string? StorageInstructions { get; set; }

    [Range(0.01, 1000)]
    public decimal? MaxDailyDose { get; set; }

    [Range(1, 168)] // 1 hour to 1 week
    public int? MinHoursBetweenDoses { get; set; }

    public bool? RequiresINRMonitoring { get; set; }

    [Range(0.5, 8.0)]
    public decimal? INRTargetMin { get; set; }

    [Range(0.5, 8.0)]
    public decimal? INRTargetMax { get; set; }
}

public sealed class UpdateMedicationRequest
{
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; set; }

    [StringLength(200)]
    public string? BrandName { get; set; }

    [StringLength(200)]
    public string? GenericName { get; set; }

    [Range(0.01, 1000)]
    public decimal? Dosage { get; set; }

    [StringLength(20, MinimumLength = 1)]
    public string? DosageUnit { get; set; }

    [StringLength(50)]
    public string? Form { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [StringLength(50)]
    public string? Shape { get; set; }

    [StringLength(100)]
    public string? Imprint { get; set; }

    public bool? IsBloodThinner { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(200)]
    public string? PrescribedBy { get; set; }

    [StringLength(200)]
    public string? Pharmacy { get; set; }

    [StringLength(1000)]
    public string? Instructions { get; set; }

    [StringLength(2000)]
    public string? SideEffects { get; set; }

    [StringLength(2000)]
    public string? Contraindications { get; set; }

    [StringLength(1000)]
    public string? StorageInstructions { get; set; }

    [Range(0.01, 1000)]
    public decimal? MaxDailyDose { get; set; }

    [Range(1, 168)] // 1 hour to 1 week
    public int? MinHoursBetweenDoses { get; set; }

    public bool? RequiresINRMonitoring { get; set; }

    [Range(0.5, 8.0)]
    public decimal? INRTargetMin { get; set; }

    [Range(0.5, 8.0)]
    public decimal? INRTargetMax { get; set; }
}

public sealed class DeactivateMedicationRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    public DateTime? EndDate { get; set; }
}
