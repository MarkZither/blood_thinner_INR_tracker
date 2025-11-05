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
using BloodThinnerTracker.Data.Shared;
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
    private readonly IApplicationDbContext _context;
    private readonly ILogger<MedicationLogsController> _logger;

    /// <summary>
    /// Initializes a new instance of the MedicationLogsController.
    /// </summary>
    /// <param name="context">Database context for medication log data access.</param>
    /// <param name="logger">Logger for operation tracking and debugging.</param>
    public MedicationLogsController(IApplicationDbContext context, ILogger<MedicationLogsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets medication logs for a specific medication.
    /// </summary>
    /// <param name="medicationPublicId">Medication PublicId (GUID).</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="includeVariance">Optional: If true, returns only logs with variance (actualDosage != expectedDosage).</param>
    /// <param name="varianceThreshold">Optional: Minimum absolute variance amount to include (e.g., 0.5 returns |actualDosage - expectedDosage| >= 0.5).</param>
    /// <returns>List of medication logs.</returns>
    /// <response code="200">Medication logs retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication not found.</response>
    [HttpGet("medication/{medicationPublicId:guid}")]
    [ProducesResponseType(typeof(List<MedicationLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MedicationLogResponse>>> GetMedicationLogs(
        Guid medicationPublicId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] MedicationLogStatus? status = null,
        [FromQuery] bool? includeVariance = null,
        [FromQuery] decimal? varianceThreshold = null)
    {
        Guid? userPublicId = null;
        try
        {
            userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to get medication logs with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            // Verify medication exists and belongs to user - get internal ID for FK queries
            var medication = await _context.Medications
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.PublicId == medicationPublicId && m.User.PublicId == userPublicId.Value && !m.IsDeleted);

            if (medication == null)
            {
                _logger.LogWarning("Medication not found: {MedicationPublicId} for user {UserPublicId}", medicationPublicId, userPublicId);
                return NotFound("Medication not found");
            }

            // ⚠️ SECURITY: Use internal int IDs for FK comparisons
            var query = _context.MedicationLogs
                .Where(ml => ml.Medication.PublicId == medication.PublicId && ml.UserId == medication.UserId && !ml.IsDeleted);

            if (fromDate.HasValue)
                query = query.Where(ml => ml.ScheduledTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(ml => ml.ScheduledTime <= toDate.Value);

            if (status.HasValue)
                query = query.Where(ml => ml.Status == status.Value);

            // T036: Variance filtering
            if (includeVariance.HasValue && includeVariance.Value)
            {
                // Filter to only logs with variance (HasVariance = true)
                query = query.Where(ml => ml.ExpectedDosage.HasValue &&
                                         ml.ActualDosage.HasValue &&
                                         Math.Abs(ml.ActualDosage.Value - ml.ExpectedDosage.Value) > 0.01m);
            }

            if (varianceThreshold.HasValue && varianceThreshold.Value > 0)
            {
                // Filter to logs where absolute variance >= threshold
                query = query.Where(ml => ml.ExpectedDosage.HasValue &&
                                         ml.ActualDosage.HasValue &&
                                         Math.Abs(ml.ActualDosage.Value - ml.ExpectedDosage.Value) >= varianceThreshold.Value);
            }

            var logs = await query
                .OrderByDescending(ml => ml.ScheduledTime)
                .Select(ml => new MedicationLogResponse
                {
                    Id = ml.PublicId.ToString(),
                    MedicationId = medication.PublicId.ToString(),
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
                    CreatedAt = ml.CreatedAt,
                    // Variance tracking fields (T035)
                    ExpectedDosage = ml.ExpectedDosage,
                    PatternDayNumber = ml.PatternDayNumber,
                    HasVariance = ml.HasVariance,
                    VarianceAmount = ml.VarianceAmount,
                    VariancePercentage = ml.VariancePercentage
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} medication logs for medication {MedicationPublicId}, user {UserPublicId}",
                logs.Count, medicationPublicId, userPublicId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medication logs for medication {MedicationPublicId}, user {UserPublicId}",
                medicationPublicId, userPublicId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving medication logs");
        }
    }

    /// <summary>
    /// Gets a specific medication log by PublicId.
    /// </summary>
    /// <param name="publicId">Medication log PublicId (GUID).</param>
    /// <returns>Medication log details.</returns>
    /// <response code="200">Medication log retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication log not found.</response>
    [HttpGet("{publicId:guid}")]
    [ProducesResponseType(typeof(MedicationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationLogResponse>> GetMedicationLog(Guid publicId)
    {
        Guid? userPublicId = null;
        try
        {
            userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to get medication log with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            // ⚠️ SECURITY: Query by PublicId and verify user ownership
            var log = await _context.MedicationLogs
                .Include(ml => ml.Medication)
                    .ThenInclude(m => m.User)
                .Where(ml => ml.PublicId == publicId && ml.Medication.User.PublicId == userPublicId.Value && !ml.IsDeleted)
                .Select(ml => new MedicationLogResponse
                {
                    Id = ml.PublicId.ToString(),
                    MedicationId = ml.Medication.PublicId.ToString(),
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
                    CreatedAt = ml.CreatedAt,
                    // Variance tracking fields (T035)
                    ExpectedDosage = ml.ExpectedDosage,
                    PatternDayNumber = ml.PatternDayNumber,
                    HasVariance = ml.HasVariance,
                    VarianceAmount = ml.VarianceAmount,
                    VariancePercentage = ml.VariancePercentage
                })
                .FirstOrDefaultAsync();

            if (log == null)
            {
                _logger.LogWarning("Medication log not found: {LogPublicId} for user {UserPublicId}", publicId, userPublicId);
                return NotFound("Medication log not found");
            }

            _logger.LogInformation("Medication log retrieved successfully: {LogPublicId} for user {UserPublicId}", publicId, userPublicId);
            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medication log {LogPublicId} for user {UserPublicId}", publicId, userPublicId);
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
        Guid? userPublicId = null;
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for medication log creation: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to log medication with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            // Parse medication PublicId from request
            if (!Guid.TryParse(request.MedicationId, out var medicationPublicId))
            {
                _logger.LogWarning("Invalid medication PublicId format: {MedicationId}", request.MedicationId);
                return BadRequest("Invalid medication ID format");
            }

            // Get user's internal Id first
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PublicId == userPublicId.Value && !u.IsDeleted);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserPublicId}", userPublicId);
                return Unauthorized("User not found");
            }

            // Get medication with patterns and verify ownership - need internal ID for FK
            // Include DosagePatterns navigation property for auto-population of expected dosage
            var medication = await _context.Medications
                .Include(m => m.DosagePatterns.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(m => m.PublicId == medicationPublicId && m.UserId == user.Id && !m.IsDeleted);

            if (medication == null)
            {
                _logger.LogWarning("Medication not found: {MedicationPublicId} for user {UserPublicId}", medicationPublicId, userPublicId);
                return NotFound("Medication not found");
            }

            // Medical safety validations
            var safetyValidation = await ValidateMedicationLogSafety(request, medication, user.Id);
            if (!safetyValidation.IsValid)
            {
                _logger.LogWarning("Medication log safety validation failed: {Errors}", string.Join(", ", safetyValidation.Errors));
                return BadRequest(new { Errors = safetyValidation.Errors });
            }

            var actualTime = request.ActualTime ?? DateTime.UtcNow;
            var scheduledTime = request.ScheduledTime ?? actualTime;

            // ⚠️ SECURITY: Generate PublicId for new entity, use internal int IDs for FKs
            var medicationLog = new MedicationLog
            {
                PublicId = Guid.NewGuid(),
                MedicationId = medication.Id,
                UserId = user.Id,
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

            // ⚠️ PATTERN AUTO-POPULATION (T033): Set expected dosage from active pattern
            // This enables variance tracking and adherence monitoring per FR-009, FR-010
            medicationLog.SetExpectedDosageFromMedication(medication);
            _logger.LogInformation("Auto-populated expected dosage: {ExpectedDosage} (PatternDay: {PatternDay}) for medication {MedicationPublicId}",
                medicationLog.ExpectedDosage, medicationLog.PatternDayNumber, medicationPublicId);

            _context.MedicationLogs.Add(medicationLog);
            await _context.SaveChangesAsync();

            var response = new MedicationLogResponse
            {
                Id = medicationLog.PublicId.ToString(),
                MedicationId = medication.PublicId.ToString(),
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
                CreatedAt = medicationLog.CreatedAt,
                // Variance tracking fields (T035)
                ExpectedDosage = medicationLog.ExpectedDosage,
                PatternDayNumber = medicationLog.PatternDayNumber,
                HasVariance = medicationLog.HasVariance,
                VarianceAmount = medicationLog.VarianceAmount,
                VariancePercentage = medicationLog.VariancePercentage
            };

            _logger.LogInformation("Medication dose logged successfully: {LogPublicId} for medication {MedicationPublicId}, user {UserPublicId}",
                medicationLog.PublicId, medicationPublicId, userPublicId);
            return CreatedAtAction(nameof(GetMedicationLog), new { publicId = medicationLog.PublicId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging medication dose for user {UserPublicId}", userPublicId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while logging the medication dose");
        }
    }

    /// <summary>
    /// Updates an existing medication log.
    /// </summary>
    /// <param name="publicId">Medication log PublicId (GUID).</param>
    /// <param name="request">Updated medication log data.</param>
    /// <returns>Updated medication log details.</returns>
    /// <response code="200">Medication log updated successfully.</response>
    /// <response code="400">Invalid medication log data provided.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication log not found.</response>
    [HttpPut("{publicId:guid}")]
    [ProducesResponseType(typeof(MedicationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationLogResponse>> UpdateMedicationLog(Guid publicId, [FromBody] UpdateMedicationLogRequest request)
    {
        Guid? userPublicId = null;
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for medication log update: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to update medication log with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            // Get existing log and verify ownership
            var existingLog = await _context.MedicationLogs
                .Include(ml => ml.Medication)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(ml => ml.PublicId == publicId && ml.Medication.User.PublicId == userPublicId.Value && !ml.IsDeleted);

            if (existingLog == null)
            {
                _logger.LogWarning("Medication log not found for update: {LogPublicId} for user {UserPublicId}", publicId, userPublicId);
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
                Id = existingLog.PublicId.ToString(),
                MedicationId = existingLog.Medication.PublicId.ToString(),
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
                CreatedAt = existingLog.CreatedAt,
                // Variance tracking fields (T035)
                ExpectedDosage = existingLog.ExpectedDosage,
                PatternDayNumber = existingLog.PatternDayNumber,
                HasVariance = existingLog.HasVariance,
                VarianceAmount = existingLog.VarianceAmount,
                VariancePercentage = existingLog.VariancePercentage
            };

            _logger.LogInformation("Medication log updated successfully: {LogPublicId} for user {UserPublicId}", publicId, userPublicId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medication log {LogPublicId} for user {UserPublicId}", publicId, userPublicId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while updating the medication log");
        }
    }

    /// <summary>
    /// Deletes a medication log (soft delete).
    /// </summary>
    /// <param name="publicId">Medication log PublicId (GUID).</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Medication log deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication log not found.</response>
    [HttpDelete("{publicId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedicationLog(Guid publicId)
    {
        Guid? userPublicId = null;
        try
        {
            userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to delete medication log with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var log = await _context.MedicationLogs
                .Include(ml => ml.Medication)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(ml => ml.PublicId == publicId && ml.Medication.User.PublicId == userPublicId.Value && !ml.IsDeleted);

            if (log == null)
            {
                _logger.LogWarning("Medication log not found for deletion: {LogPublicId} for user {UserPublicId}", publicId, userPublicId);
                return NotFound("Medication log not found");
            }

            // Soft delete
            log.IsDeleted = true;
            log.DeletedAt = DateTime.UtcNow;
            log.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Medication log deleted successfully: {LogPublicId} for user {UserPublicId}", publicId, userPublicId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting medication log {LogPublicId} for user {UserPublicId}", publicId, userPublicId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while deleting the medication log");
        }
    }

    /// <summary>
    /// Validates medication log safety parameters.
    /// </summary>
    /// <param name="request">Medication log request.</param>
    /// <param name="medication">Medication being logged.</param>
    /// <param name="userId">User's internal ID (int).</param>
    /// <returns>Validation result with any safety concerns.</returns>
    private async Task<SafetyValidationResult> ValidateMedicationLogSafety(LogMedicationRequest request, Medication medication, int userId)
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
            // ⚠️ SECURITY: Use internal int IDs for FK comparisons
            var lastDose = await _context.MedicationLogs
                .Where(ml => ml.Medication.PublicId == medication.PublicId &&
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

        // ⚠️ SECURITY: Use internal int IDs for FK comparisons
        var todayDoses = await _context.MedicationLogs
            .Where(ml => ml.Medication.PublicId == medication.PublicId &&
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

    // Variance tracking fields (T035)
    /// <summary>
    /// Expected dosage from the active pattern on ScheduledTime date.
    /// NULL if no pattern was active.
    /// </summary>
    public decimal? ExpectedDosage { get; set; }

    /// <summary>
    /// Position in the dosage pattern cycle (1-based).
    /// Example: Day 3 of a 6-day pattern.
    /// NULL if no pattern was active.
    /// </summary>
    public int? PatternDayNumber { get; set; }

    /// <summary>
    /// Indicates whether actual dosage differs from expected dosage (variance > 0.01mg).
    /// </summary>
    public bool HasVariance { get; set; }

    /// <summary>
    /// Variance amount (actual - expected).
    /// Positive = took more than expected, negative = took less.
    /// NULL if no expected dosage is set.
    /// </summary>
    public decimal? VarianceAmount { get; set; }

    /// <summary>
    /// Variance percentage ((actual - expected) / expected * 100).
    /// Example: -25% means took 25% less than expected.
    /// NULL if no expected dosage or expected dosage is 0.
    /// </summary>
    public decimal? VariancePercentage { get; set; }
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
