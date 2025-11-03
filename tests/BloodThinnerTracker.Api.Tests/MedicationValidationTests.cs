//-----------------------------------------------------------------------
// <copyright file="MedicationValidationTests.cs" company="Blood Thinner Tracker">
//     Copyright (c) Blood Thinner Tracker. All rights reserved.
//     MEDICAL DISCLAIMER: This software is for informational purposes only.
//     Always consult with qualified healthcare professionals for medical decisions.
// </copyright>
//-----------------------------------------------------------------------

using BloodThinnerTracker.Shared.Models;
using BloodThinnerTracker.Api.Controllers;
using System.Reflection;

namespace BloodThinnerTracker.Api.Tests;

/// <summary>
/// Unit tests for medication validation logic to prevent regressions.
/// These tests ensure medication safety validations work correctly, especially for:
/// - Warfarin (VitKAntagonist) requiring INR monitoring
/// - DOACs not requiring INR monitoring
/// - Dosage limits specific to medication types
/// </summary>
public sealed class MedicationValidationTests
{
    #region Warfarin (VitKAntagonist) Validation Tests

    /// <summary>
    /// Test: Warfarin MUST require INR monitoring - cannot be disabled.
    /// This is critical for patient safety as Warfarin has variable effects.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_WarfarinWithoutINRMonitoring_ReturnsError()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Warfarin",
            Type = MedicationType.VitKAntagonist,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = false, // ❌ INVALID - Warfarin MUST have INR monitoring
            MinHoursBetweenDoses = 24
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.False(result.IsValid, "Warfarin without INR monitoring should be invalid");
        Assert.Contains(result.Errors, e => e.Contains("Warfarin") && e.Contains("requires INR monitoring"));
    }

    /// <summary>
    /// Test: Warfarin with INR monitoring enabled should pass validation.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_WarfarinWithINRMonitoring_IsValid()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Warfarin",
            Type = MedicationType.VitKAntagonist,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true, // ✅ VALID - Warfarin requires INR monitoring
            INRTargetMin = 2.0m,
            INRTargetMax = 3.0m,
            MinHoursBetweenDoses = 24,
            MaxDailyDose = 10
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.True(result.IsValid, $"Warfarin with INR monitoring should be valid. Errors: {string.Join(", ", result.Errors)}");
    }

    /// <summary>
    /// Test: Warfarin should require INR target range to be set.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_WarfarinWithoutINRTargetRange_ReturnsError()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Warfarin",
            Type = MedicationType.VitKAntagonist,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true,
            // ❌ INVALID - Missing INR target range
            INRTargetMin = null,
            INRTargetMax = null,
            MinHoursBetweenDoses = 24
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.False(result.IsValid, "Warfarin without INR target range should be invalid");
        Assert.Contains(result.Errors, e => e.Contains("INR target range") && e.Contains("2.0-3.0"));
    }

    /// <summary>
    /// Test: Warfarin dosage above 20mg should trigger warning.
    /// High doses require special medical attention.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_WarfarinAbove20mg_ReturnsError()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Warfarin",
            Type = MedicationType.VitKAntagonist,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true,
            INRTargetMin = 2.0m,
            INRTargetMax = 3.0m,
            MaxDailyDose = 25, // ❌ INVALID - Above 20mg limit
            MinHoursBetweenDoses = 24
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.False(result.IsValid, "Warfarin above 20mg should be invalid");
        Assert.Contains(result.Errors, e => e.Contains("Warfarin") && e.Contains("20mg"));
    }

    /// <summary>
    /// Test: Warfarin at exactly 20mg should be valid (boundary test).
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_WarfarinAt20mg_IsValid()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Warfarin",
            Type = MedicationType.VitKAntagonist,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true,
            INRTargetMin = 2.0m,
            INRTargetMax = 3.0m,
            MaxDailyDose = 20, // ✅ VALID - Exactly at limit
            MinHoursBetweenDoses = 24
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.True(result.IsValid, $"Warfarin at 20mg should be valid. Errors: {string.Join(", ", result.Errors)}");
    }

    #endregion

    #region DOAC Validation Tests

    /// <summary>
    /// Test: DOACs (Direct Oral Anticoagulants) should NOT require INR monitoring.
    /// DOACs have predictable effects and don't need INR monitoring.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_DOACWithoutINRMonitoring_IsValid()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Eliquis",
            Type = MedicationType.DOAC,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.TwiceDaily,
            RequiresINRMonitoring = false, // ✅ VALID - DOACs don't need INR monitoring
            MinHoursBetweenDoses = 12,
            MaxDailyDose = 10
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.True(result.IsValid, $"DOAC without INR monitoring should be valid. Errors: {string.Join(", ", result.Errors)}");
    }

    /// <summary>
    /// Test: DOACs can have INR monitoring enabled if requested (optional).
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_DOACWithOptionalINRMonitoring_IsValid()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Xarelto",
            Type = MedicationType.DOAC,
            IsBloodThinner = true,
            Dosage = 20,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true, // ✅ VALID - Optional for DOACs
            MinHoursBetweenDoses = 24,
            MaxDailyDose = 20
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.True(result.IsValid, $"DOAC with optional INR monitoring should be valid. Errors: {string.Join(", ", result.Errors)}");
    }

    #endregion

    #region Blood Thinner General Validation Tests

    /// <summary>
    /// Test: Blood thinners require minimum 12 hours between doses.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_BloodThinnerWith11HoursBetweenDoses_ReturnsError()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Warfarin",
            Type = MedicationType.VitKAntagonist,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true,
            INRTargetMin = 2.0m,
            INRTargetMax = 3.0m,
            MinHoursBetweenDoses = 11, // ❌ INVALID - Below 12 hour minimum
            MaxDailyDose = 10
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.False(result.IsValid, "Blood thinner with <12 hours between doses should be invalid");
        Assert.Contains(result.Errors, e => e.Contains("12 hours between doses"));
    }

    /// <summary>
    /// Test: Blood thinners with exactly 12 hours between doses should be valid.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_BloodThinnerWith12HoursBetweenDoses_IsValid()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Eliquis",
            Type = MedicationType.DOAC,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.TwiceDaily,
            RequiresINRMonitoring = false,
            MinHoursBetweenDoses = 12, // ✅ VALID - Exactly at minimum
            MaxDailyDose = 10
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.True(result.IsValid, $"Blood thinner with 12 hours between doses should be valid. Errors: {string.Join(", ", result.Errors)}");
    }

    #endregion

    #region INR Range Validation Tests

    /// <summary>
    /// Test: INR target range must be between 0.5 and 8.0.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_INRRangeOutsideLimits_ReturnsError()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Warfarin",
            Type = MedicationType.VitKAntagonist,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true,
            INRTargetMin = 0.3m, // ❌ INVALID - Below 0.5
            INRTargetMax = 9.0m, // ❌ INVALID - Above 8.0
            MinHoursBetweenDoses = 24,
            MaxDailyDose = 10
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.False(result.IsValid, "INR range outside 0.5-8.0 should be invalid");
        Assert.Contains(result.Errors, e => e.Contains("0.5 and 8.0"));
    }

    /// <summary>
    /// Test: INR minimum must be less than maximum.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_INRMinGreaterThanMax_ReturnsError()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Warfarin",
            Type = MedicationType.VitKAntagonist,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true,
            INRTargetMin = 3.0m, // ❌ INVALID - Greater than max
            INRTargetMax = 2.0m,
            MinHoursBetweenDoses = 24,
            MaxDailyDose = 10
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.False(result.IsValid, "INR min >= max should be invalid");
        Assert.Contains(result.Errors, e => e.Contains("minimum must be less than maximum"));
    }

    /// <summary>
    /// Test: Standard therapeutic INR range (2.0-3.0) should be valid.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_StandardINRRange_IsValid()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Warfarin",
            Type = MedicationType.VitKAntagonist,
            IsBloodThinner = true,
            Dosage = 5,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = true,
            INRTargetMin = 2.0m, // ✅ VALID - Standard range
            INRTargetMax = 3.0m,
            MinHoursBetweenDoses = 24,
            MaxDailyDose = 10
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.True(result.IsValid, $"Standard INR range should be valid. Errors: {string.Join(", ", result.Errors)}");
    }

    #endregion

    #region General Medication Validation Tests

    /// <summary>
    /// Test: Medication dosage must be greater than 0.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_ZeroDosage_ReturnsError()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Test Medication",
            Type = MedicationType.Other,
            IsBloodThinner = false,
            Dosage = 0, // ❌ INVALID - Zero dosage
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.False(result.IsValid, "Zero dosage should be invalid");
        Assert.Contains(result.Errors, e => e.Contains("dosage must be greater than 0"));
    }

    /// <summary>
    /// Test: Non-blood thinner medication without INR monitoring should be valid.
    /// </summary>
    [Fact]
    public void ValidateMedicationSafety_NonBloodThinnerWithoutINR_IsValid()
    {
        // Arrange
        var request = new CreateMedicationRequest
        {
            Name = "Aspirin",
            Type = MedicationType.Other,
            IsBloodThinner = false,
            Dosage = 81,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            RequiresINRMonitoring = false,
            MinHoursBetweenDoses = 24
        };

        // Act
        var result = InvokeValidateMedicationSafety(request);

        // Assert
        Assert.True(result.IsValid, $"Non-blood thinner should be valid. Errors: {string.Join(", ", result.Errors)}");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Uses reflection to invoke the private ValidateMedicationSafety method.
    /// This allows us to test the validation logic without making the method public.
    /// </summary>
    private static ValidationResult InvokeValidateMedicationSafety(CreateMedicationRequest request)
    {
        var controllerType = typeof(MedicationsController);
        var method = controllerType.GetMethod("ValidateMedicationSafety",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException("ValidateMedicationSafety method not found");
        }

        var result = method.Invoke(null, new object[] { request });
        return (ValidationResult)result!;
    }

    #endregion
}
