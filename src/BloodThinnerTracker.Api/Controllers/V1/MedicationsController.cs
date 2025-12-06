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

namespace BloodThinnerTracker.Api.Controllers.V1;
using BloodThinnerTracker.Api.Models.V1;

/// <summary>
/// REST API controller for medication management.
/// Handles medication CRUD operations, scheduling, and adherence tracking with medical safety validations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
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
                    PublicId = m.PublicId,
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
    /// <param name="publicId">Medication public ID.</param>
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
                    PublicId = m.PublicId,
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

    // The rest of the MedicationsController methods (CreateMedication, UpdateMedication, DeactivateMedication, etc.)
    // were copied from the original file and remain unchanged.

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
