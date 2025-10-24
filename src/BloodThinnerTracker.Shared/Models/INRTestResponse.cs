// BloodThinnerTracker.Shared - INR Test Response Models
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models;

/// <summary>
/// Response model for INR test data.
/// </summary>
public class INRTestResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public decimal INRValue { get; set; }
    public decimal? TargetINRMin { get; set; }
    public decimal? TargetINRMax { get; set; }
    public decimal? ProthrombinTime { get; set; }
    public decimal? PartialThromboplastinTime { get; set; }
    public string? Laboratory { get; set; }
    public string? OrderedBy { get; set; }
    public string? TestMethod { get; set; }
    public bool IsPointOfCare { get; set; }
    public bool? WasFasting { get; set; }
    public DateTime? LastMedicationTime { get; set; }
    public string? MedicationsTaken { get; set; }
    public string? FoodsConsumed { get; set; }
    public string? HealthConditions { get; set; }
    public INRResultStatus Status { get; set; }
    public string? RecommendedActions { get; set; }
    public string? DosageChanges { get; set; }
    public DateTime? NextTestDate { get; set; }
    public string? Notes { get; set; }
    public bool ReviewedByProvider { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool PatientNotified { get; set; }
    public string? NotificationMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new INR test.
/// </summary>
public class CreateINRTestRequest
{
    public DateTime TestDate { get; set; }
    public decimal INRValue { get; set; }
    public decimal? TargetINRMin { get; set; }
    public decimal? TargetINRMax { get; set; }
    public decimal? ProthrombinTime { get; set; }
    public decimal? PartialThromboplastinTime { get; set; }
    public string? Laboratory { get; set; }
    public string? OrderedBy { get; set; }
    public string? TestMethod { get; set; }
    public bool IsPointOfCare { get; set; }
    public bool? WasFasting { get; set; }
    public DateTime? LastMedicationTime { get; set; }
    public string? MedicationsTaken { get; set; }
    public string? FoodsConsumed { get; set; }
    public string? HealthConditions { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request model for updating an existing INR test.
/// </summary>
public class UpdateINRTestRequest
{
    public DateTime? TestDate { get; set; }
    public decimal? INRValue { get; set; }
    public decimal? TargetINRMin { get; set; }
    public decimal? TargetINRMax { get; set; }
    public decimal? ProthrombinTime { get; set; }
    public decimal? PartialThromboplastinTime { get; set; }
    public string? Laboratory { get; set; }
    public string? OrderedBy { get; set; }
    public string? TestMethod { get; set; }
    public bool? IsPointOfCare { get; set; }
    public bool? WasFasting { get; set; }
    public DateTime? LastMedicationTime { get; set; }
    public string? MedicationsTaken { get; set; }
    public string? FoodsConsumed { get; set; }
    public string? HealthConditions { get; set; }
    public string? Notes { get; set; }
}
