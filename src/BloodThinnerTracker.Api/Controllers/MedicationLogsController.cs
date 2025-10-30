/*
 * BloodThinnerTracker.Api - Medication Logs Controller
 * Licensed under MIT License. See LICENSE file in the project root.
 * 
 * REST API controller for medication dose logging and adherence tracking.
 * Provides endpoints for logging when medications are taken and viewing history.
 * 
 * ⚠️ MEDICAL DATA CONTROLLER:
 * This controller handles medication intake records which are protected health information (PHI).
 * All operations must comply with healthcare data protection regulations and include proper
 * authentication, authorization, audit logging, and medical safety validations.
 * 
 * ⚠️ MEDICATION SAFETY WARNINGS:
 * - 12-hour minimum between doses for blood thinners
 * - Maximum daily dose validation enforced
 * - Cannot log future doses
 * - Missed dose tracking for adherence monitoring
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
using BloodThinnerTracker.Api.Data;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Controllers;

/// <summary>
/// REST API controller for medication dose logging and adherence tracking.
/// Handles recording when medications are taken and viewing medication history.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public sealed class MedicationLogsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicationLogsController> _logger;

    /// <summary>
    /// Initializes a new instance of the MedicationLogsController.
    /// </summary>
    /// <param name="context">Database context for medication log data access.</param>
    /// <param name="logger">Logger for operation tracking and debugging.</param>
    public MedicationLogsController(ApplicationDbContext context, ILogger<MedicationLogsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets medication logs for a specific medication.
    /// </summary>
    /// <param name="medicationId">Medication ID.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>List of medication logs.</returns>
    /// <response code="200">Medication logs retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication not found.</response>
    [HttpGet("medication/{medicationId}")]
    [ProducesResponseType(typeof(List<MedicationLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MedicationLogResponse>>> GetMedicationLogs(
        string medicationId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] MedicationLogStatus? status = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Attempted to get medication logs with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            // Verify medication exists and belongs to user
            var medication = await _context.Medications
                .FirstOrDefaultAsync(m => m.Id == medicationId && m.UserId == userId && !m.IsDeleted);

            if (medication == null)
            {
                _logger.LogWarning("Medication not found: {MedicationId} for user ID: {UserId}", medicationId, userId);
                return NotFound("Medication not found");
            }

            var query = _context.MedicationLogs
                .Where(ml => ml.MedicationId == medicationId && ml.UserId == userId && !ml.IsDeleted);

            if (fromDate.HasValue)
                query = query.Where(ml => ml.ScheduledTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(ml => ml.ScheduledTime <= toDate.Value);

            if (status.HasValue)
                query = query.Where(ml => ml.Status == status.Value);

            var logs = await query
                .OrderByDescending(ml => ml.ScheduledTime)
                .Select(ml => new MedicationLogResponse
                {
                    Id = ml.Id,
                    MedicationId = ml.MedicationId,
                    MedicationName = ml.Medication.Name,
                    ScheduledTime = ml.ScheduledTime,
                    ActualTime = ml.ActualTime,
                    Status = ml.Status,
                    ActualDosage = ml.ActualDosage,
                    ActualDosageUnit = ml.ActualDosageUnit,
                    Reason = ml.Reason,
                    SideEffects = ml.SideEffects,
                    Notes = ml.Notes,
                    TakenWithFood = ml.TakenWithFood,
                    FoodDetails = ml.FoodDetails,
                    TimeVarianceMinutes = ml.TimeVarianceMinutes,
                    CreatedAt = ml.CreatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} medication logs for medication {MedicationId}, user ID: {UserId}", 
                logs.Count, medicationId, userId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medication logs for medication {MedicationId}, user ID: {UserId}", 
                medicationId, GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while retrieving medication logs");
        }
    }

    /// <summary>
    /// Gets a specific medication log by ID.
    /// </summary>
    /// <param name="id">Medication log ID.</param>
    /// <returns>Medication log details.</returns>
    /// <response code="200">Medication log retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication log not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MedicationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationLogResponse>> GetMedicationLog(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Attempted to get medication log with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var log = await _context.MedicationLogs
                .Include(ml => ml.Medication)
                .Where(ml => ml.Id == id && ml.UserId == userId && !ml.IsDeleted)
                .Select(ml => new MedicationLogResponse
                {
                    Id = ml.Id,
                    MedicationId = ml.MedicationId,
                    MedicationName = ml.Medication.Name,
                    ScheduledTime = ml.ScheduledTime,
                    ActualTime = ml.ActualTime,
                    Status = ml.Status,
                    ActualDosage = ml.ActualDosage,
                    ActualDosageUnit = ml.ActualDosageUnit,
                    Reason = ml.Reason,
                    SideEffects = ml.SideEffects,
                    Notes = ml.Notes,
                    TakenWithFood = ml.TakenWithFood,
                    FoodDetails = ml.FoodDetails,
                    TimeVarianceMinutes = ml.TimeVarianceMinutes,
                    CreatedAt = ml.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (log == null)
            {
                _logger.LogWarning("Medication log not found: {LogId} for user ID: {UserId}", id, userId);
                return NotFound("Medication log not found");
            }

            _logger.LogInformation("Medication log retrieved successfully: {LogId} for user ID: {UserId}", id, userId);
            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medication log {LogId} for user ID: {UserId}", id, GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while retrieving the medication log");
        }
    }

    /// <summary>
    /// Logs a medication dose (records that medication was taken).
    /// </summary>
    /// <param name="request">Medication log data.</param>
    /// <returns>Created medication log details.</returns>
    /// <response code="201">Medication dose logged successfully.</response>
    /// <response code="400">Invalid medication log data provided.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MedicationLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MedicationLogResponse>> LogMedicationDose([FromBody] LogMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for medication log creation: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Attempted to log medication with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            // Get medication and verify ownership
            var medication = await _context.Medications
                .FirstOrDefaultAsync(m => m.Id == request.MedicationId && m.UserId == userId && !m.IsDeleted);

            if (medication == null)
            {
                _logger.LogWarning("Medication not found: {MedicationId} for user ID: {UserId}", request.MedicationId, userId);
                return NotFound("Medication not found");
            }

            // Medical safety validations
            var safetyValidation = await ValidateMedicationLogSafety(request, medication, userId);
            if (!safetyValidation.IsValid)
            {
                _logger.LogWarning("Medication log safety validation failed: {Errors}", string.Join(", ", safetyValidation.Errors));
                return BadRequest(new { Errors = safetyValidation.Errors });
            }

            var actualTime = request.ActualTime ?? DateTime.UtcNow;
            var scheduledTime = request.ScheduledTime ?? actualTime;

            var medicationLog = new MedicationLog
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                MedicationId = request.MedicationId,
                ScheduledTime = scheduledTime,
                ActualTime = actualTime,
                Status = request.Status ?? MedicationLogStatus.Taken,
                ActualDosage = request.ActualDosage ?? medication.Dosage,
                ActualDosageUnit = request.ActualDosageUnit ?? medication.DosageUnit,
                Reason = request.Reason,
                SideEffects = request.SideEffects,
                Notes = request.Notes,
                TakenWithFood = request.TakenWithFood,
                FoodDetails = request.FoodDetails,
                TimeVarianceMinutes = (int)(actualTime - scheduledTime).TotalMinutes,
                EntryMethod = LogEntryMethod.Manual,
                EntryDevice = "Web Application",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.MedicationLogs.Add(medicationLog);
            await _context.SaveChangesAsync();

            var response = new MedicationLogResponse
            {
                Id = medicationLog.Id,
                MedicationId = medicationLog.MedicationId,
                MedicationName = medication.Name,
                ScheduledTime = medicationLog.ScheduledTime,
                ActualTime = medicationLog.ActualTime,
                Status = medicationLog.Status,
                ActualDosage = medicationLog.ActualDosage,
                ActualDosageUnit = medicationLog.ActualDosageUnit,
                Reason = medicationLog.Reason,
                SideEffects = medicationLog.SideEffects,
                Notes = medicationLog.Notes,
                TakenWithFood = medicationLog.TakenWithFood,
                FoodDetails = medicationLog.FoodDetails,
                TimeVarianceMinutes = medicationLog.TimeVarianceMinutes,
                CreatedAt = medicationLog.CreatedAt
            };

            _logger.LogInformation("Medication dose logged successfully: {LogId} for medication {MedicationId}, user ID: {UserId}", 
                medicationLog.Id, request.MedicationId, userId);
            return CreatedAtAction(nameof(GetMedicationLog), new { id = medicationLog.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging medication dose for user ID: {UserId}", GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while logging the medication dose");
        }
    }

    /// <summary>
    /// Updates an existing medication log.
    /// </summary>
    /// <param name="id">Medication log ID.</param>
    /// <param name="request">Updated medication log data.</param>
    /// <returns>Updated medication log details.</returns>
    /// <response code="200">Medication log updated successfully.</response>
    /// <response code="400">Invalid medication log data provided.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication log not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(MedicationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationLogResponse>> UpdateMedicationLog(string id, [FromBody] UpdateMedicationLogRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for medication log update: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Attempted to update medication log with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            // Get existing log and verify ownership
            var existingLog = await _context.MedicationLogs
                .Include(ml => ml.Medication)
                .FirstOrDefaultAsync(ml => ml.Id == id && ml.UserId == userId && !ml.IsDeleted);

            if (existingLog == null)
            {
                _logger.LogWarning("Medication log not found for update: {LogId} for user ID: {UserId}", id, userId);
                return NotFound("Medication log not found");
            }

            // Update fields
            if (request.ActualTime.HasValue)
            {
                existingLog.ActualTime = request.ActualTime.Value;
                existingLog.TimeVarianceMinutes = (int)(request.ActualTime.Value - existingLog.ScheduledTime).TotalMinutes;
            }

            if (request.Status.HasValue)
                existingLog.Status = request.Status.Value;

            if (request.ActualDosage.HasValue)
                existingLog.ActualDosage = request.ActualDosage.Value;

            if (!string.IsNullOrWhiteSpace(request.ActualDosageUnit))
                existingLog.ActualDosageUnit = request.ActualDosageUnit;

            if (request.Reason != null)
                existingLog.Reason = request.Reason;

            if (request.SideEffects != null)
                existingLog.SideEffects = request.SideEffects;

            if (request.Notes != null)
                existingLog.Notes = request.Notes;

            if (request.TakenWithFood.HasValue)
                existingLog.TakenWithFood = request.TakenWithFood.Value;

            if (request.FoodDetails != null)
                existingLog.FoodDetails = request.FoodDetails;

            existingLog.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new MedicationLogResponse
            {
                Id = existingLog.Id,
                MedicationId = existingLog.MedicationId,
                MedicationName = existingLog.Medication.Name,
                ScheduledTime = existingLog.ScheduledTime,
                ActualTime = existingLog.ActualTime,
                Status = existingLog.Status,
                ActualDosage = existingLog.ActualDosage,
                ActualDosageUnit = existingLog.ActualDosageUnit,
                Reason = existingLog.Reason,
                SideEffects = existingLog.SideEffects,
                Notes = existingLog.Notes,
                TakenWithFood = existingLog.TakenWithFood,
                FoodDetails = existingLog.FoodDetails,
                TimeVarianceMinutes = existingLog.TimeVarianceMinutes,
                CreatedAt = existingLog.CreatedAt
            };

            _logger.LogInformation("Medication log updated successfully: {LogId} for user ID: {UserId}", id, userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medication log {LogId} for user ID: {UserId}", id, GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while updating the medication log");
        }
    }

    /// <summary>
    /// Deletes a medication log (soft delete).
    /// </summary>
    /// <param name="id">Medication log ID.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Medication log deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication log not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedicationLog(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Attempted to delete medication log with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var log = await _context.MedicationLogs
                .FirstOrDefaultAsync(ml => ml.Id == id && ml.UserId == userId && !ml.IsDeleted);

            if (log == null)
            {
                _logger.LogWarning("Medication log not found for deletion: {LogId} for user ID: {UserId}", id, userId);
                return NotFound("Medication log not found");
            }

            // Soft delete
            log.IsDeleted = true;
            log.DeletedAt = DateTime.UtcNow;
            log.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Medication log deleted successfully: {LogId} for user ID: {UserId}", id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting medication log {LogId} for user ID: {UserId}", id, GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while deleting the medication log");
        }
    }

    /// <summary>
    /// Validates medication log safety parameters.
    /// </summary>
    /// <param name="request">Medication log request.</param>
    /// <param name="medication">Medication being logged.</param>
    /// <param name="userId">User ID.</param>
    /// <returns>Validation result with any safety concerns.</returns>
    private async Task<SafetyValidationResult> ValidateMedicationLogSafety(LogMedicationRequest request, Medication medication, string userId)
    {
        var errors = new List<string>();
        var actualTime = request.ActualTime ?? DateTime.UtcNow;

        // Cannot log future doses
        if (actualTime > DateTime.UtcNow.AddMinutes(5)) // 5-minute grace period for clock skew
        {
            errors.Add("Cannot log medication doses in the future");
        }

        // Check minimum time between doses for blood thinners
        if (medication.IsBloodThinner && medication.MinHoursBetweenDoses > 0)
        {
            var lastDose = await _context.MedicationLogs
                .Where(ml => ml.MedicationId == request.MedicationId && 
                            ml.UserId == userId && 
                            ml.Status == MedicationLogStatus.Taken &&
                            ml.ActualTime.HasValue &&
                            ml.ActualTime.Value < actualTime && // Only check doses that are actually before this one
                            !ml.IsDeleted)
                .OrderByDescending(ml => ml.ActualTime)
                .FirstOrDefaultAsync();

            if (lastDose != null && lastDose.ActualTime.HasValue)
            {
                var hoursSinceLastDose = (actualTime - lastDose.ActualTime.Value).TotalHours;
                
                // Add 2-hour grace period for flexibility (e.g., 12 hours becomes 10 hours minimum)
                var minimumHoursWithGrace = Math.Max(medication.MinHoursBetweenDoses - 2, medication.MinHoursBetweenDoses * 0.8);
                
                if (hoursSinceLastDose < minimumHoursWithGrace)
                {
                    // This is a hard error - too soon
                    errors.Add($"Too soon for next dose. Minimum {medication.MinHoursBetweenDoses} hours required between doses for this blood thinner. " +
                              $"Last dose was {hoursSinceLastDose:F1} hours ago. Please wait at least {(minimumHoursWithGrace - hoursSinceLastDose):F1} more hours.");
                }
                else if (hoursSinceLastDose < medication.MinHoursBetweenDoses)
                {
                    // This is a warning but allowed (within grace period)
                    _logger.LogWarning("Dose logged within grace period: {Hours} hours since last dose (minimum: {Min})", 
                        hoursSinceLastDose, medication.MinHoursBetweenDoses);
                }
            }
        }

        // Check daily dose limit
        var today = actualTime.Date;
        var todayStart = today.ToUniversalTime();
        var todayEnd = today.AddDays(1).ToUniversalTime();

        var todayDoses = await _context.MedicationLogs
            .Where(ml => ml.MedicationId == request.MedicationId &&
                        ml.UserId == userId &&
                        ml.Status == MedicationLogStatus.Taken &&
                        ml.ActualTime.HasValue &&
                        ml.ActualTime.Value >= todayStart &&
                        ml.ActualTime.Value < todayEnd &&
                        !ml.IsDeleted)
            .ToListAsync();

        var totalDosageToday = todayDoses.Sum(ml => ml.ActualDosage ?? 0);
        var newDosage = request.ActualDosage ?? medication.Dosage;
        
        if (totalDosageToday + newDosage > medication.MaxDailyDose)
        {
            errors.Add($"Daily dose limit ({medication.MaxDailyDose}{medication.DosageUnit}) would be exceeded. " +
                      $"Already taken: {totalDosageToday}{medication.DosageUnit} today.");
        }

        // Validate dosage is reasonable
        if (request.ActualDosage.HasValue)
        {
            if (request.ActualDosage.Value <= 0)
            {
                errors.Add("Actual dosage must be greater than 0");
            }

            if (request.ActualDosage.Value > medication.MaxDailyDose)
            {
                errors.Add($"Single dose ({request.ActualDosage.Value}{request.ActualDosageUnit}) " +
                          $"exceeds maximum daily dose ({medication.MaxDailyDose}{medication.DosageUnit})");
            }
        }

        return new SafetyValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Gets the current user ID from JWT claims.
    /// </summary>
    /// <returns>Current user ID or null if not authenticated.</returns>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               User.FindFirst("sub")?.Value ??
               User.FindFirst("userId")?.Value;
    }
}

/// <summary>
/// Response model for medication log data.
/// </summary>
public sealed class MedicationLogResponse
{
    public string Id { get; set; } = string.Empty;
    public string MedicationId { get; set; } = string.Empty;
    public string MedicationName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public DateTime? ActualTime { get; set; }
    public MedicationLogStatus Status { get; set; }
    public decimal? ActualDosage { get; set; }
    public string? ActualDosageUnit { get; set; }
    public string? Reason { get; set; }
    public string? SideEffects { get; set; }
    public string? Notes { get; set; }
    public bool? TakenWithFood { get; set; }
    public string? FoodDetails { get; set; }
    public int TimeVarianceMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for logging a medication dose.
/// </summary>
public sealed class LogMedicationRequest
{
    [Required]
    public string MedicationId { get; set; } = string.Empty;

    public DateTime? ScheduledTime { get; set; }

    public DateTime? ActualTime { get; set; }

    public MedicationLogStatus? Status { get; set; }

    [Range(0.01, 1000)]
    public decimal? ActualDosage { get; set; }

    [StringLength(20)]
    public string? ActualDosageUnit { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(500)]
    public string? SideEffects { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool? TakenWithFood { get; set; }

    [StringLength(200)]
    public string? FoodDetails { get; set; }
}

/// <summary>
/// Request model for updating a medication log.
/// </summary>
public sealed class UpdateMedicationLogRequest
{
    public DateTime? ActualTime { get; set; }

    public MedicationLogStatus? Status { get; set; }

    [Range(0.01, 1000)]
    public decimal? ActualDosage { get; set; }

    [StringLength(20)]
    public string? ActualDosageUnit { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(500)]
    public string? SideEffects { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool? TakenWithFood { get; set; }

    [StringLength(200)]
    public string? FoodDetails { get; set; }
}

/// <summary>
/// Result of medication log safety validation.
/// </summary>
internal sealed class SafetyValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
