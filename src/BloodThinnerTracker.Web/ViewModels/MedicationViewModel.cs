/*
 * BloodThinnerTracker.Web - Medication View Model
 * Licensed under MIT License. See LICENSE file in the project root.
 *
 * View model for medication add/edit forms with comprehensive validation.
 * Supports schedule configuration, inventory tracking, and medical safety checks.
 */

using System.ComponentModel.DataAnnotations;

namespace BloodThinnerTracker.Web.ViewModels;

/// <summary>
/// View model for medication add and edit forms with validation.
/// </summary>
public class MedicationViewModel
{
    // Basic Information

    [Required(ErrorMessage = "Medication name is required")]
    [StringLength(200, ErrorMessage = "Medication name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Dosage is required")]
    [Range(0.01, 10000, ErrorMessage = "Dosage must be between 0.01 and 10000")]
    public decimal Dosage { get; set; } = 5.0m;

    [Required(ErrorMessage = "Dosage unit is required")]
    [StringLength(50, ErrorMessage = "Dosage unit cannot exceed 50 characters")]
    public string DosageUnit { get; set; } = "mg";

    [StringLength(100, ErrorMessage = "Brand name cannot exceed 100 characters")]
    public string? BrandName { get; set; }

    [StringLength(100, ErrorMessage = "Generic name cannot exceed 100 characters")]
    public string? GenericName { get; set; }

    [StringLength(50, ErrorMessage = "Form cannot exceed 50 characters")]
    public string? Form { get; set; }

    [StringLength(50, ErrorMessage = "Color cannot exceed 50 characters")]
    public string? Color { get; set; }

    [StringLength(100, ErrorMessage = "Shape cannot exceed 100 characters")]
    public string? Shape { get; set; }

    [StringLength(100, ErrorMessage = "Imprint cannot exceed 100 characters")]
    public string? Imprint { get; set; }

    // Schedule Information

    [Required(ErrorMessage = "Frequency is required")]
    [StringLength(100, ErrorMessage = "Frequency cannot exceed 100 characters")]
    public string Frequency { get; set; } = "Once Daily";

    public List<TimeSpan> ScheduledTimes { get; set; } = new() { new TimeSpan(8, 0, 0) };

    [StringLength(500, ErrorMessage = "Timing instructions cannot exceed 500 characters")]
    public string? TimingInstructions { get; set; }

    public DateTime? StartDate { get; set; } = DateTime.Today;

    public DateTime? EndDate { get; set; }

    public bool AsNeeded { get; set; }

    [StringLength(200, ErrorMessage = "As-needed instructions cannot exceed 200 characters")]
    public string? AsNeededInstructions { get; set; }

    // Prescriber Information

    [StringLength(200, ErrorMessage = "Prescriber name cannot exceed 200 characters")]
    public string? PrescriberName { get; set; }

    [StringLength(100, ErrorMessage = "Prescriber specialty cannot exceed 100 characters")]
    public string? PrescriberSpecialty { get; set; }

    [StringLength(100, ErrorMessage = "Prescriber phone cannot exceed 100 characters")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public string? PrescriberPhone { get; set; }

    [StringLength(200, ErrorMessage = "Clinic name cannot exceed 200 characters")]
    public string? ClinicName { get; set; }

    [StringLength(500, ErrorMessage = "Clinic address cannot exceed 500 characters")]
    public string? ClinicAddress { get; set; }

    // Safety Information

    [StringLength(50, ErrorMessage = "Indication cannot exceed 50 characters")]
    public string? Indication { get; set; }

    [StringLength(2000, ErrorMessage = "Side effects cannot exceed 2000 characters")]
    public string? SideEffects { get; set; }

    [StringLength(2000, ErrorMessage = "Warnings cannot exceed 2000 characters")]
    public string? Warnings { get; set; }

    [StringLength(2000, ErrorMessage = "Interactions cannot exceed 2000 characters")]
    public string? Interactions { get; set; }

    [StringLength(1000, ErrorMessage = "Precautions cannot exceed 1000 characters")]
    public string? Precautions { get; set; }

    [StringLength(1000, ErrorMessage = "Storage instructions cannot exceed 1000 characters")]
    public string? StorageInstructions { get; set; }

    // Inventory Tracking

    [Range(0, int.MaxValue, ErrorMessage = "Quantity on hand cannot be negative")]
    public int? QuantityOnHand { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Refill threshold must be at least 1")]
    public int? RefillThresholdDays { get; set; }

    [StringLength(100, ErrorMessage = "Pharmacy name cannot exceed 100 characters")]
    public string? PharmacyName { get; set; }

    [StringLength(100, ErrorMessage = "Pharmacy phone cannot exceed 100 characters")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public string? PharmacyPhone { get; set; }

    [StringLength(50, ErrorMessage = "Prescription number cannot exceed 50 characters")]
    public string? PrescriptionNumber { get; set; }

    [Range(0, 100, ErrorMessage = "Refills remaining must be between 0 and 100")]
    public int? RefillsRemaining { get; set; }

    public DateTime? LastRefillDate { get; set; }

    public DateTime? NextRefillDate { get; set; }

    // Additional Information

    [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Validates that scheduled times are unique and in valid format.
    /// </summary>
    public bool AreScheduledTimesValid()
    {
        if (ScheduledTimes == null || !ScheduledTimes.Any())
            return false;

        // Check for duplicates
        var uniqueTimes = ScheduledTimes.Distinct().Count();
        return uniqueTimes == ScheduledTimes.Count;
    }

    /// <summary>
    /// Validates that end date is after start date if both are provided.
    /// </summary>
    public bool IsDateRangeValid()
    {
        if (StartDate.HasValue && EndDate.HasValue)
        {
            return EndDate.Value >= StartDate.Value;
        }
        return true;
    }

    /// <summary>
    /// Validates that refill threshold is reasonable for the medication schedule.
    /// </summary>
    public bool IsRefillThresholdReasonable()
    {
        if (!RefillThresholdDays.HasValue || !QuantityOnHand.HasValue)
            return true;

        // Threshold should not exceed quantity on hand
        return RefillThresholdDays.Value <= QuantityOnHand.Value;
    }

    /// <summary>
    /// Gets a display string for the medication schedule.
    /// </summary>
    public string GetScheduleDisplayText()
    {
        if (!ScheduledTimes.Any())
            return Frequency;

        var times = string.Join(", ", ScheduledTimes.OrderBy(t => t).Select(t => t.ToString(@"hh\:mm")));
        return $"{Frequency} at {times}";
    }
}
