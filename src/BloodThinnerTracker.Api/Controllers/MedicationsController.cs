/*
 * BloodThinnerTracker.Api - Medications Controller
 * Licensed under MIT License. See LICENSE file in the project root.
 *
 * REST API controller for medication management in the blood thinner tracking system.
 * Provides endpoints for medication CRUD operations, scheduling, and adherence tracking.
 *
 * ⚠️ MEDICAL DATA CONTROLLER:
 * This controller handles medication information which is protected health information (PHI).
 * All operations must comply with healthcare data protection regulations and include proper
 * authentication, authorization, audit logging, and medical safety validations.
 *
 * ⚠️ MEDICATION SAFETY WARNINGS:
 * - Warfarin dosage above 20mg requires special attention
 * - Proper frequency and timing validation enforced
 * - Drug interaction checking recommended
 * - Healthcare provider approval required for changes
 *
 * IMPORTANT MEDICAL DISCLAIMER:
 * This software is for informational purposes only and should not replace
 * professional medical advice. Users should consult healthcare providers
 * for medication decisions and dosage adjustments.
 */

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BloodThinnerTracker.Data.Shared;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Controllers;

/// <summary>
/// REST API controller for medication management.
/// Handles medication CRUD operations, scheduling, and adherence tracking with medical safety validations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public sealed class MedicationsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<MedicationsController> _logger;

    /// <summary>
    /// Initializes a new instance of the MedicationsController.
    /// </summary>
    /// <param name="context">Database context for medication data access.</param>
    /// <param name="logger">Logger for operation tracking and debugging.</param>
    public MedicationsController(IApplicationDbContext context, ILogger<MedicationsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all medications for the current user.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive medications.</param>
    /// <returns>List of user's medications.</returns>
    /// <response code="200">Medications retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<MedicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<MedicationResponse>>> GetMedications([FromQuery] bool includeInactive = false)
    {
        try
        {
            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to get medications with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var query = _context.Medications
                .Include(m => m.User)
                .Where(m => m.User.PublicId == userPublicId.Value && !m.IsDeleted);

            if (!includeInactive)
            {
                query = query.Where(m => m.IsActive);
            }

            var medications = await query
                .OrderBy(m => m.Name)
                .Select(m => new MedicationResponse
                {
                    PublicId = m.PublicId.ToString(),
                    Name = m.Name,
                    BrandName = m.BrandName,
                    GenericName = m.GenericName,
                    Dosage = m.Dosage,
                    DosageUnit = m.DosageUnit,
                    Type = m.Type,
                    Frequency = m.Frequency,
                    CustomFrequency = m.CustomFrequency,
                    ScheduledTimes = m.ScheduledTimes,
                    IsActive = m.IsActive,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    PrescribedBy = m.PrescribedBy,
                    PrescriptionDate = m.PrescriptionDate,
                    Pharmacy = m.Pharmacy,
                    PrescriptionNumber = m.PrescriptionNumber,
                    Instructions = m.Instructions,
                    SideEffects = m.SideEffects,
                    FoodInteractions = m.FoodInteractions,
                    DrugInteractions = m.DrugInteractions,
                    RemindersEnabled = m.RemindersEnabled,
                    ReminderMinutes = m.ReminderMinutes,
                    Notes = m.Notes,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} medications for user {UserPublicId}", medications.Count, userPublicId);
            return Ok(medications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medications");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving medications");
        }
    }

    /// <summary>
    /// Gets a specific medication by ID.
    /// </summary>
    /// <param name="id">Medication ID.</param>
    /// <returns>Medication details.</returns>
    /// <response code="200">Medication retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication not found.</response>
    [HttpGet("{publicId:guid}")]
    [ProducesResponseType(typeof(MedicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationResponse>> GetMedication(Guid publicId)
    {
        try
        {
            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to get medication with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var medication = await _context.Medications
                .Include(m => m.User)
                .Where(m => m.PublicId == publicId && m.User.PublicId == userPublicId.Value && !m.IsDeleted)
                .Select(m => new MedicationResponse
                {
                    PublicId = m.PublicId.ToString(),
                    Name = m.Name,
                    BrandName = m.BrandName,
                    GenericName = m.GenericName,
                    Dosage = m.Dosage,
                    DosageUnit = m.DosageUnit,
                    Type = m.Type,
                    Frequency = m.Frequency,
                    Form = m.Form ?? string.Empty,
                    Color = m.Color,
                    Shape = m.Shape,
                    Imprint = m.Imprint,
                    IsBloodThinner = m.IsBloodThinner,
                    IsActive = m.IsActive,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    PrescribedBy = m.PrescribedBy,
                    Pharmacy = m.Pharmacy,
                    Instructions = m.Instructions,
                    SideEffects = m.SideEffects,
                    Contraindications = m.Contraindications,
                    StorageInstructions = m.StorageInstructions,
                    MaxDailyDose = m.MaxDailyDose,
                    MinHoursBetweenDoses = m.MinHoursBetweenDoses,
                    RequiresINRMonitoring = m.RequiresINRMonitoring,
                    INRTargetMin = m.INRTargetMin,
                    INRTargetMax = m.INRTargetMax,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (medication == null)
            {
                _logger.LogWarning("Medication not found: {MedicationPublicId} for user {UserPublicId}", publicId, userPublicId);
                return NotFound("Medication not found");
            }

            _logger.LogInformation("Medication retrieved successfully: {MedicationPublicId} for user {UserPublicId}", publicId, userPublicId);
            return Ok(medication);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medication {MedicationPublicId}", publicId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving the medication");
        }
    }

    /// <summary>
    /// Creates a new medication for the current user.
    /// </summary>
    /// <param name="request">Medication creation data.</param>
    /// <returns>Created medication details.</returns>
    /// <response code="201">Medication created successfully.</response>
    /// <response code="400">Invalid medication data provided.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MedicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MedicationResponse>> CreateMedication([FromBody] CreateMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for medication creation: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to create medication with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            // Get the user's internal ID for foreign key relationship
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PublicId == userPublicId.Value && !u.IsDeleted);

            if (user == null)
            {
                return Unauthorized("User not found");
            }

            // Medical safety validations
            var validationResult = ValidateMedicationSafety(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Medication safety validation failed: {Errors}", string.Join(", ", validationResult.Errors));
                return BadRequest(new { Errors = validationResult.Errors });
            }

            var medication = new Medication
            {
                UserId = user.Id,  // ⚠️ SECURITY: Use internal int ID for FK
                PublicId = Guid.NewGuid(),  // ⚠️ SECURITY: Generate non-sequential public ID
                Name = request.Name.Trim(),
                BrandName = request.BrandName?.Trim(),
                GenericName = request.GenericName?.Trim(),
                Type = request.Type,
                Dosage = request.Dosage,
                DosageUnit = request.DosageUnit.Trim(),
                Frequency = request.Frequency,
                ScheduledTimes = request.ScheduledTimes,
                Form = request.Form?.Trim() ?? "Tablet",
                Color = request.Color?.Trim(),
                Shape = request.Shape?.Trim(),
                Imprint = request.Imprint?.Trim(),
                IsBloodThinner = request.IsBloodThinner,
                IsActive = true,
                StartDate = request.StartDate ?? DateTime.UtcNow.Date,
                EndDate = request.EndDate,
                PrescribedBy = request.PrescribedBy?.Trim(),
                Pharmacy = request.Pharmacy?.Trim(),
                Instructions = request.Instructions?.Trim(),
                SideEffects = request.SideEffects?.Trim(),
                Contraindications = request.Contraindications?.Trim(),
                StorageInstructions = request.StorageInstructions?.Trim(),
                MaxDailyDose = request.MaxDailyDose ?? (request.IsBloodThinner ? 20 : 100),
                MinHoursBetweenDoses = Math.Max(1, request.MinHoursBetweenDoses ?? (request.IsBloodThinner ? 12 : 4)),
                RequiresINRMonitoring = request.RequiresINRMonitoring ?? request.IsBloodThinner,
                INRTargetMin = request.INRTargetMin,
                INRTargetMax = request.INRTargetMax,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Medications.Add(medication);
            await _context.SaveChangesAsync();

            var response = new MedicationResponse
            {
                PublicId = medication.PublicId.ToString(),
                Name = medication.Name,
                BrandName = medication.BrandName,
                GenericName = medication.GenericName,
                Dosage = medication.Dosage,
                DosageUnit = medication.DosageUnit,
                Type = medication.Type,
                Frequency = medication.Frequency,
                Form = medication.Form,
                Color = medication.Color,
                Shape = medication.Shape,
                Imprint = medication.Imprint,
                IsBloodThinner = medication.IsBloodThinner,
                IsActive = medication.IsActive,
                StartDate = medication.StartDate,
                EndDate = medication.EndDate,
                PrescribedBy = medication.PrescribedBy,
                Pharmacy = medication.Pharmacy,
                Instructions = medication.Instructions,
                SideEffects = medication.SideEffects,
                Contraindications = medication.Contraindications,
                StorageInstructions = medication.StorageInstructions,
                MaxDailyDose = medication.MaxDailyDose,
                MinHoursBetweenDoses = medication.MinHoursBetweenDoses,
                RequiresINRMonitoring = medication.RequiresINRMonitoring,
                INRTargetMin = medication.INRTargetMin,
                INRTargetMax = medication.INRTargetMax,
                CreatedAt = medication.CreatedAt,
                UpdatedAt = medication.UpdatedAt
            };

            _logger.LogInformation("Medication created successfully: {MedicationPublicId} for user {UserPublicId}", medication.PublicId, userPublicId);
            return CreatedAtAction(nameof(GetMedication), new { publicId = medication.PublicId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating medication");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while creating the medication");
        }
    }

    /// <summary>
    /// Updates an existing medication.
    /// </summary>
    /// <param name="id">Medication ID.</param>
    /// <param name="request">Updated medication data.</param>
    /// <returns>Updated medication details.</returns>
    /// <response code="200">Medication updated successfully.</response>
    /// <response code="400">Invalid medication data provided.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication not found.</response>
    [HttpPut("{publicId:guid}")]
    [ProducesResponseType(typeof(MedicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationResponse>> UpdateMedication(Guid publicId, [FromBody] UpdateMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for medication update: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to update medication with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var medication = await _context.Medications
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.PublicId == publicId && m.User.PublicId == userPublicId.Value && !m.IsDeleted);

            if (medication == null)
            {
                _logger.LogWarning("Medication not found for update: {MedicationPublicId} for user {UserPublicId}", publicId, userPublicId);
                return NotFound("Medication not found");
            }

            // Medical safety validations for updates
            var validationResult = ValidateMedicationUpdateSafety(request, medication);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Medication update safety validation failed: {Errors}", string.Join(", ", validationResult.Errors));
                return BadRequest(new { Errors = validationResult.Errors });
            }

            // Update medication fields
            if (!string.IsNullOrWhiteSpace(request.Name))
                medication.Name = request.Name.Trim();

            medication.BrandName = request.BrandName?.Trim();
            medication.GenericName = request.GenericName?.Trim();

            if (request.Dosage.HasValue)
                medication.Dosage = request.Dosage.Value;

            if (!string.IsNullOrWhiteSpace(request.DosageUnit))
                medication.DosageUnit = request.DosageUnit.Trim();

            medication.Form = request.Form?.Trim() ?? medication.Form;
            medication.Color = request.Color?.Trim();
            medication.Shape = request.Shape?.Trim();
            medication.Imprint = request.Imprint?.Trim();

            if (request.IsBloodThinner.HasValue)
                medication.IsBloodThinner = request.IsBloodThinner.Value;

            if (request.IsActive.HasValue)
                medication.IsActive = request.IsActive.Value;

            if (request.StartDate.HasValue)
                medication.StartDate = request.StartDate.Value;

            medication.EndDate = request.EndDate;
            medication.PrescribedBy = request.PrescribedBy?.Trim();
            medication.Pharmacy = request.Pharmacy?.Trim();
            medication.Instructions = request.Instructions?.Trim();
            medication.SideEffects = request.SideEffects?.Trim();
            medication.Contraindications = request.Contraindications?.Trim();
            medication.StorageInstructions = request.StorageInstructions?.Trim();

            if (request.MaxDailyDose.HasValue)
                medication.MaxDailyDose = request.MaxDailyDose.Value;

            if (request.MinHoursBetweenDoses.HasValue)
                medication.MinHoursBetweenDoses = Math.Max(1, request.MinHoursBetweenDoses.Value);

            if (request.RequiresINRMonitoring.HasValue)
                medication.RequiresINRMonitoring = request.RequiresINRMonitoring.Value;

            medication.INRTargetMin = request.INRTargetMin;
            medication.INRTargetMax = request.INRTargetMax;
            medication.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new MedicationResponse
            {
                PublicId = medication.PublicId.ToString(),
                Name = medication.Name,
                BrandName = medication.BrandName,
                GenericName = medication.GenericName,
                Dosage = medication.Dosage,
                DosageUnit = medication.DosageUnit,
                Type = medication.Type,
                Frequency = medication.Frequency,
                Form = medication.Form,
                Color = medication.Color,
                Shape = medication.Shape,
                Imprint = medication.Imprint,
                IsBloodThinner = medication.IsBloodThinner,
                IsActive = medication.IsActive,
                StartDate = medication.StartDate,
                EndDate = medication.EndDate,
                PrescribedBy = medication.PrescribedBy,
                Pharmacy = medication.Pharmacy,
                Instructions = medication.Instructions,
                SideEffects = medication.SideEffects,
                Contraindications = medication.Contraindications,
                StorageInstructions = medication.StorageInstructions,
                MaxDailyDose = medication.MaxDailyDose,
                MinHoursBetweenDoses = medication.MinHoursBetweenDoses,
                RequiresINRMonitoring = medication.RequiresINRMonitoring,
                INRTargetMin = medication.INRTargetMin,
                INRTargetMax = medication.INRTargetMax,
                CreatedAt = medication.CreatedAt,
                UpdatedAt = medication.UpdatedAt
            };

            _logger.LogInformation("Medication updated successfully: {MedicationPublicId} for user {UserPublicId}", publicId, userPublicId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medication {MedicationPublicId}", publicId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while updating the medication");
        }
    }

    /// <summary>
    /// Deactivates a medication (soft delete).
    /// </summary>
    /// <param name="id">Medication ID.</param>
    /// <param name="request">Deactivation request with reason.</param>
    /// <returns>Confirmation of medication deactivation.</returns>
    /// <response code="200">Medication deactivated successfully.</response>
    /// <response code="400">Invalid deactivation request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication not found.</response>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeactivateMedication(Guid publicId, [FromBody] DeactivateMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for medication deactivation: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to deactivate medication with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var medication = await _context.Medications
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.PublicId == publicId && m.User.PublicId == userPublicId.Value && !m.IsDeleted);

            if (medication == null)
            {
                _logger.LogWarning("Medication not found for deactivation: {MedicationPublicId} for user {UserPublicId}", publicId, userPublicId);
                return NotFound("Medication not found");
            }

            // Deactivate the medication
            medication.IsActive = false;
            medication.EndDate = DateTime.UtcNow.Date;
            medication.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Medication deactivated: {MedicationPublicId} for user {UserPublicId}, Reason: {Reason}",
                publicId, userPublicId, request.Reason);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Medication has been deactivated successfully. Historical data has been preserved.",
                Data = new { DeactivatedAt = medication.UpdatedAt, EndDate = medication.EndDate }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating medication {MedicationPublicId}", publicId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while deactivating the medication");
        }
    }

    /// <summary>
    /// Gets blood thinner medications for the current user.
    /// </summary>
    /// <returns>List of blood thinner medications.</returns>
    /// <response code="200">Blood thinner medications retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("blood-thinners")]
    [ProducesResponseType(typeof(List<MedicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<MedicationResponse>>> GetBloodThinners()
    {
        try
        {
            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to get blood thinners with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var bloodThinners = await _context.Medications
                .Include(m => m.User)
                .Where(m => m.User.PublicId == userPublicId.Value && !m.IsDeleted && m.IsBloodThinner && m.IsActive)
                .OrderBy(m => m.Name)
                .Select(m => new MedicationResponse
                {
                    PublicId = m.PublicId.ToString(),
                    Name = m.Name,
                    BrandName = m.BrandName,
                    GenericName = m.GenericName,
                    Dosage = m.Dosage,
                    DosageUnit = m.DosageUnit,
                    Type = m.Type,
                    Frequency = m.Frequency,
                    Form = m.Form ?? string.Empty,
                    Color = m.Color,
                    Shape = m.Shape,
                    Imprint = m.Imprint,
                    IsBloodThinner = m.IsBloodThinner,
                    IsActive = m.IsActive,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    PrescribedBy = m.PrescribedBy,
                    Pharmacy = m.Pharmacy,
                    Instructions = m.Instructions,
                    SideEffects = m.SideEffects,
                    Contraindications = m.Contraindications,
                    StorageInstructions = m.StorageInstructions,
                    MaxDailyDose = m.MaxDailyDose,
                    MinHoursBetweenDoses = m.MinHoursBetweenDoses,
                    RequiresINRMonitoring = m.RequiresINRMonitoring,
                    INRTargetMin = m.INRTargetMin,
                    INRTargetMax = m.INRTargetMax,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} blood thinner medications for user {UserPublicId}", bloodThinners.Count, userPublicId);
            return Ok(bloodThinners);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blood thinner medications");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving blood thinner medications");
        }
    }

    /// <summary>
    /// Validates medication safety parameters for creation.
    /// </summary>
    /// <param name="request">Medication creation request.</param>
    /// <returns>Validation result with any safety concerns.</returns>
    private static ValidationResult ValidateMedicationSafety(CreateMedicationRequest request)
    {
        var errors = new List<string>();

        // Blood thinner specific validations
        if (request.IsBloodThinner)
        {
            if (request.MinHoursBetweenDoses < 12)
            {
                errors.Add("Blood thinners require minimum 12 hours between doses for safety");
            }

            // Warfarin (VitKAntagonist) specific validations
            if (request.Type == MedicationType.VitKAntagonist)
            {
                if (request.MaxDailyDose > 20)
                {
                    errors.Add("Warfarin dosage above 20mg requires special attention. Consult healthcare provider.");
                }

                // Warfarin MUST have INR monitoring
                if (request.RequiresINRMonitoring == false)
                {
                    errors.Add("Warfarin (Vitamin K Antagonist) requires INR monitoring. This cannot be disabled.");
                }

                // Warfarin should have INR targets set
                if (!request.INRTargetMin.HasValue || !request.INRTargetMax.HasValue)
                {
                    errors.Add("Warfarin requires INR target range to be set (typical: 2.0-3.0)");
                }
            }

            // INR range validation (when specified)
            if (request.INRTargetMin.HasValue && request.INRTargetMax.HasValue)
            {
                if (request.INRTargetMin.Value < 0.5m || request.INRTargetMax.Value > 8.0m)
                {
                    errors.Add("INR target range must be between 0.5 and 8.0");
                }

                if (request.INRTargetMin.Value >= request.INRTargetMax.Value)
                {
                    errors.Add("INR target minimum must be less than maximum");
                }
            }
        }

        // General medication validations
        if (request.Dosage <= 0)
        {
            errors.Add("Medication dosage must be greater than 0");
        }

        if (request.MaxDailyDose <= 0)
        {
            errors.Add("Maximum daily dose must be greater than 0");
        }

        if (request.MinHoursBetweenDoses < 1)
        {
            errors.Add("Minimum hours between doses must be at least 1 hour");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Validates medication safety parameters for updates.
    /// </summary>
    /// <param name="request">Medication update request.</param>
    /// <param name="existingMedication">Existing medication data.</param>
    /// <returns>Validation result with any safety concerns.</returns>
    private static ValidationResult ValidateMedicationUpdateSafety(UpdateMedicationRequest request, Medication existingMedication)
    {
        var errors = new List<string>();

        // Check if this is a blood thinner (existing or being changed to one)
        var isBloodThinner = request.IsBloodThinner ?? existingMedication.IsBloodThinner;
        var medicationType = existingMedication.Type; // Type cannot be changed via update

        if (isBloodThinner)
        {
            var minHours = request.MinHoursBetweenDoses ?? existingMedication.MinHoursBetweenDoses;
            if (minHours < 12)
            {
                errors.Add("Blood thinners require minimum 12 hours between doses for safety");
            }

            // Warfarin (VitKAntagonist) specific validations
            if (medicationType == MedicationType.VitKAntagonist)
            {
                var maxDose = request.MaxDailyDose ?? existingMedication.MaxDailyDose;
                if (maxDose > 20)
                {
                    errors.Add("Warfarin dosage above 20mg requires special attention. Consult healthcare provider.");
                }

                // Warfarin MUST have INR monitoring
                var requiresINR = request.RequiresINRMonitoring ?? existingMedication.RequiresINRMonitoring;
                if (!requiresINR)
                {
                    errors.Add("Warfarin (Vitamin K Antagonist) requires INR monitoring. This cannot be disabled.");
                }
            }

            // INR range validation (when specified)
            var inrMin = request.INRTargetMin ?? existingMedication.INRTargetMin;
            var inrMax = request.INRTargetMax ?? existingMedication.INRTargetMax;

            if (inrMin.HasValue && inrMax.HasValue)
            {
                if (inrMin.Value < 0.5m || inrMax.Value > 8.0m)
                {
                    errors.Add("INR target range must be between 0.5 and 8.0");
                }

                if (inrMin.Value >= inrMax.Value)
                {
                    errors.Add("INR target minimum must be less than maximum");
                }
            }
        }

        // General validations
        if (request.Dosage.HasValue && request.Dosage.Value <= 0)
        {
            errors.Add("Medication dosage must be greater than 0");
        }

        if (request.MaxDailyDose.HasValue && request.MaxDailyDose.Value <= 0)
        {
            errors.Add("Maximum daily dose must be greater than 0");
        }

        if (request.MinHoursBetweenDoses.HasValue && request.MinHoursBetweenDoses.Value < 1)
        {
            errors.Add("Minimum hours between doses must be at least 1 hour");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Gets the current user's public ID (GUID) from JWT claims.
    /// ⚠️ SECURITY: JWT claims contain PublicId (GUID), never internal database Id.
    /// </summary>
    /// <returns>Current user's public GUID or null if not authenticated.</returns>
    private Guid? GetCurrentUserPublicId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("sub")?.Value ??
                        User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            return null;

        return Guid.TryParse(userIdStr, out var guid) ? guid : null;
    }
}

/// <summary>
/// Validation result for medication safety checks.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Response model for medication data.
/// </summary>
public sealed class MedicationResponse
{
    public string PublicId { get; set; } = string.Empty;
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

/// <summary>
/// Request model for creating a new medication.
/// </summary>
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

/// <summary>
/// Request model for updating an existing medication.
/// </summary>
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

/// <summary>
/// Request model for deactivating a medication.
/// </summary>
public sealed class DeactivateMedicationRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    public DateTime? EndDate { get; set; }
}
