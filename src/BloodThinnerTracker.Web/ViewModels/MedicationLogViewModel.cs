/*
 * BloodThinnerTracker.Web - Medication Log View Model
 * Licensed under MIT License. See LICENSE file in the project root.
 * 
 * View model for medication dose logging forms.
 * Handles form binding and validation for logging medication doses.
 */

using System.ComponentModel.DataAnnotations;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Web.ViewModels;

/// <summary>
/// View model for medication dose logging forms.
/// Provides form binding and validation for recording medication intake.
/// </summary>
public sealed class MedicationLogViewModel
{
    /// <summary>
    /// Medication ID (required).
    /// </summary>
    [Required(ErrorMessage = "Medication is required")]
    public string MedicationId { get; set; } = string.Empty;

    /// <summary>
    /// Medication name (display only).
    /// </summary>
    public string MedicationName { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled time for this dose.
    /// </summary>
    [Required(ErrorMessage = "Scheduled time is required")]
    public DateTime ScheduledTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Actual time the dose was taken (defaults to now).
    /// </summary>
    public DateTime ActualTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Status of the medication log.
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    public MedicationLogStatus Status { get; set; } = MedicationLogStatus.Taken;

    /// <summary>
    /// Actual dosage taken (defaults to prescribed dosage).
    /// </summary>
    [Range(0.01, 1000, ErrorMessage = "Dosage must be between 0.01 and 1000")]
    public decimal? ActualDosage { get; set; }

    /// <summary>
    /// Unit of the actual dosage.
    /// </summary>
    [StringLength(20, ErrorMessage = "Dosage unit cannot exceed 20 characters")]
    public string? ActualDosageUnit { get; set; }

    /// <summary>
    /// Reason for skipping or modifying the dose.
    /// </summary>
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }

    /// <summary>
    /// Any side effects experienced.
    /// </summary>
    [StringLength(500, ErrorMessage = "Side effects description cannot exceed 500 characters")]
    public string? SideEffects { get; set; }

    /// <summary>
    /// Additional notes about the dose.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether the medication was taken with food.
    /// </summary>
    public bool? TakenWithFood { get; set; }

    /// <summary>
    /// Details about food taken with medication (important for Warfarin).
    /// </summary>
    [StringLength(200, ErrorMessage = "Food details cannot exceed 200 characters")]
    public string? FoodDetails { get; set; }

    /// <summary>
    /// Prescribed dosage (reference, display only).
    /// </summary>
    public decimal PrescribedDosage { get; set; }

    /// <summary>
    /// Prescribed dosage unit (reference, display only).
    /// </summary>
    public string PrescribedDosageUnit { get; set; } = string.Empty;

    /// <summary>
    /// Minimum hours required between doses (display only).
    /// </summary>
    public int MinHoursBetweenDoses { get; set; }

    /// <summary>
    /// Maximum daily dose allowed (display only).
    /// </summary>
    public decimal MaxDailyDose { get; set; }

    /// <summary>
    /// Whether this is a blood thinner medication (affects validations).
    /// </summary>
    public bool IsBloodThinner { get; set; }

    /// <summary>
    /// Calculates the time variance in minutes from scheduled time.
    /// </summary>
    /// <returns>Minutes difference (positive = late, negative = early).</returns>
    public int CalculateTimeVariance()
    {
        return (int)(ActualTime - ScheduledTime).TotalMinutes;
    }

    /// <summary>
    /// Checks if the dose is considered late (more than 60 minutes from scheduled).
    /// </summary>
    /// <returns>True if late.</returns>
    public bool IsLate()
    {
        return CalculateTimeVariance() > 60;
    }

    /// <summary>
    /// Checks if the dose is within the acceptable time window.
    /// </summary>
    /// <param name="acceptableWindowMinutes">Acceptable variance in minutes (default 120 = 2 hours).</param>
    /// <returns>True if within window.</returns>
    public bool IsWithinAcceptableWindow(int acceptableWindowMinutes = 120)
    {
        return Math.Abs(CalculateTimeVariance()) <= acceptableWindowMinutes;
    }

    /// <summary>
    /// Gets a user-friendly status message based on time variance.
    /// </summary>
    /// <returns>Status message.</returns>
    public string GetTimingMessage()
    {
        var variance = CalculateTimeVariance();
        if (Math.Abs(variance) <= 15)
            return "On time";
        else if (variance > 0)
            return $"Late by {variance} minutes";
        else
            return $"Early by {Math.Abs(variance)} minutes";
    }

    /// <summary>
    /// Validates if the actual dosage is reasonable compared to prescribed.
    /// </summary>
    /// <returns>Validation message, or null if valid.</returns>
    public string? ValidateDosage()
    {
        if (!ActualDosage.HasValue)
            return null;

        if (ActualDosage.Value <= 0)
            return "Dosage must be greater than 0";

        if (ActualDosage.Value > MaxDailyDose)
            return $"Dosage ({ActualDosage.Value}{ActualDosageUnit}) exceeds maximum daily dose ({MaxDailyDose}{PrescribedDosageUnit})";

        // Warn if significantly different from prescribed
        var variance = Math.Abs(ActualDosage.Value - PrescribedDosage);
        var percentVariance = (variance / PrescribedDosage) * 100;
        
        if (percentVariance > 50)
            return $"Warning: Dosage differs significantly from prescribed ({PrescribedDosage}{PrescribedDosageUnit})";

        return null;
    }

    /// <summary>
    /// Resets the form to default values for a new log entry.
    /// </summary>
    /// <param name="medication">Medication to log a dose for.</param>
    public void Reset(Medication medication)
    {
        MedicationId = medication.Id;
        MedicationName = medication.Name;
        ScheduledTime = DateTime.Now;
        ActualTime = DateTime.Now;
        Status = MedicationLogStatus.Taken;
        ActualDosage = medication.Dosage;
        ActualDosageUnit = medication.DosageUnit;
        PrescribedDosage = medication.Dosage;
        PrescribedDosageUnit = medication.DosageUnit;
        MinHoursBetweenDoses = medication.MinHoursBetweenDoses;
        MaxDailyDose = medication.MaxDailyDose;
        IsBloodThinner = medication.IsBloodThinner;
        Reason = null;
        SideEffects = null;
        Notes = null;
        TakenWithFood = null;
        FoodDetails = null;
    }

    /// <summary>
    /// Loads data from an existing medication log for editing.
    /// </summary>
    /// <param name="log">Medication log to load.</param>
    /// <param name="medication">Associated medication.</param>
    public void LoadFromLog(Services.MedicationLogDto log, Medication medication)
    {
        MedicationId = log.MedicationId;
        MedicationName = log.MedicationName;
        ScheduledTime = log.ScheduledTime;
        ActualTime = log.ActualTime ?? DateTime.Now;
        Status = log.Status;
        ActualDosage = log.ActualDosage;
        ActualDosageUnit = log.ActualDosageUnit;
        PrescribedDosage = medication.Dosage;
        PrescribedDosageUnit = medication.DosageUnit;
        MinHoursBetweenDoses = medication.MinHoursBetweenDoses;
        MaxDailyDose = medication.MaxDailyDose;
        IsBloodThinner = medication.IsBloodThinner;
        Reason = log.Reason;
        SideEffects = log.SideEffects;
        Notes = log.Notes;
        TakenWithFood = log.TakenWithFood;
        FoodDetails = log.FoodDetails;
    }

    /// <summary>
    /// Converts view model to DTO for API submission.
    /// </summary>
    /// <returns>Medication log DTO.</returns>
    public Services.MedicationLogDto ToDto()
    {
        return new Services.MedicationLogDto
        {
            MedicationId = MedicationId,
            MedicationName = MedicationName,
            ScheduledTime = ScheduledTime,
            ActualTime = ActualTime,
            Status = Status,
            ActualDosage = ActualDosage,
            ActualDosageUnit = ActualDosageUnit,
            Reason = Reason,
            SideEffects = SideEffects,
            Notes = Notes,
            TakenWithFood = TakenWithFood,
            FoodDetails = FoodDetails,
            TimeVarianceMinutes = CalculateTimeVariance()
        };
    }
}
