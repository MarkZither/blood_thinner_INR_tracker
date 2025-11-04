namespace BloodThinnerTracker.Api.Controllers;

using BloodThinnerTracker.Api.Validators;
using BloodThinnerTracker.Data.Shared;
using BloodThinnerTracker.Shared.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

/// <summary>
/// API controller for managing medication dosage patterns.
/// Enables creating, retrieving, and modifying variable-dosage schedules with temporal tracking.
/// </summary>
/// <remarks>
/// ⚠️ MEDICAL DATA: All endpoints require authentication and enforce user-medication ownership.
/// Pattern modifications preserve historical accuracy for past medication logs.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/medications/{medicationId}/patterns")]
[Produces("application/json")]
public class MedicationPatternsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<MedicationPatternsController> _logger;
    private readonly IValidator<CreateDosagePatternRequest> _validator;

    public MedicationPatternsController(
        IApplicationDbContext context,
        ILogger<MedicationPatternsController> logger,
        IValidator<CreateDosagePatternRequest> validator)
    {
        _context = context;
        _logger = logger;
        _validator = validator;
    }

    /// <summary>
    /// Creates a new dosage pattern for a medication.
    /// Optionally closes the previous active pattern by setting its EndDate.
    /// </summary>
    /// <param name="medicationId">ID of the medication to add pattern to</param>
    /// <param name="request">Pattern details including sequence, dates, and notes</param>
    /// <returns>The created dosage pattern</returns>
    /// <response code="201">Pattern created successfully</response>
    /// <response code="400">Validation failed (invalid pattern, overlapping dates)</response>
    /// <response code="403">User doesn't have access to this medication</response>
    /// <response code="404">Medication not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(DosagePatternResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DosagePatternResponse>> CreatePattern(
        int medicationId,
        [FromBody] CreateDosagePatternRequest request)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
            });
        }

        var userPublicId = GetCurrentUserPublicId();
        if (userPublicId == null)
        {
            _logger.LogWarning("Attempted to create pattern with invalid user ID");
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication required",
                Detail = "User identity not found in token"
            });
        }

        // Verify medication exists and user has access
        var medication = await _context.Medications
            .Include(m => m.User)
            .Include(m => m.DosagePatterns)
            .FirstOrDefaultAsync(m => m.Id == medicationId && m.User.PublicId == userPublicId.Value && !m.IsDeleted);

        if (medication == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Medication not found",
                Detail = $"Medication with ID {medicationId} does not exist or you don't have access."
            });
        }

        // Medication-specific validation (Warfarin)
        var medicationValidation = CreateDosagePatternRequestValidator.ValidateMedicationSpecificRules(
            request,
            medication.Name,
            medication.Type.ToString());

        if (!medicationValidation.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Medication-specific validation failed",
                Detail = string.Join("; ", medicationValidation.Errors.Select(e => e.ErrorMessage))
            });
        }

        // Check for overlapping patterns if not closing previous
        if (!request.ClosePreviousPattern)
        {
            var hasOverlap = medication.DosagePatterns.Any(p =>
                p.EndDate == null || // Active pattern exists
                (request.EndDate == null && p.EndDate >= request.StartDate) || // New pattern is indefinite and overlaps
                (request.EndDate != null && p.StartDate <= request.EndDate && (p.EndDate == null || p.EndDate >= request.StartDate))); // Date range overlap

            if (hasOverlap)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Pattern overlap detected",
                    Detail = "A pattern already exists for the specified date range. Set closePreviousPattern=true to automatically close the previous pattern."
                });
            }
        }

        // Close previous active pattern if requested
        if (request.ClosePreviousPattern)
        {
            var activePattern = medication.DosagePatterns
                .Where(p => p.EndDate == null || p.EndDate >= request.StartDate)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefault();

            if (activePattern != null)
            {
                activePattern.EndDate = request.StartDate.AddDays(-1);
                activePattern.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Closed previous pattern {PatternId} for medication {MedicationId}, EndDate set to {EndDate}",
                    activePattern.Id, medicationId, activePattern.EndDate);
            }
        }

        // Create new pattern
        var newPattern = new MedicationDosagePattern
        {
            MedicationId = medicationId,
            PatternSequence = request.PatternSequence,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MedicationDosagePatterns.Add(newPattern);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created dosage pattern {PatternId} for medication {MedicationId}, pattern length {Length}, start date {StartDate}",
            newPattern.Id, medicationId, newPattern.PatternLength, newPattern.StartDate);

        var response = MapToResponse(newPattern);
        return CreatedAtAction(
            nameof(GetActivePattern),
            new { medicationId },
            response);
    }

    /// <summary>
    /// Retrieves the currently active dosage pattern for a medication.
    /// Active pattern is defined as EndDate = NULL.
    /// </summary>
    /// <param name="medicationId">ID of the medication</param>
    /// <returns>The active dosage pattern, or 404 if no active pattern exists</returns>
    /// <response code="200">Active pattern found</response>
    /// <response code="403">User doesn't have access to this medication</response>
    /// <response code="404">Medication not found or no active pattern exists</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(DosagePatternResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DosagePatternResponse>> GetActivePattern(int medicationId)
    {
        var userPublicId = GetCurrentUserPublicId();
        if (userPublicId == null)
        {
            _logger.LogWarning("Attempted to get active pattern with invalid user ID");
            return Unauthorized();
        }

        // Verify medication exists and user has access
        var medicationExists = await _context.Medications
            .Include(m => m.User)
            .AnyAsync(m => m.Id == medicationId && m.User.PublicId == userPublicId.Value && !m.IsDeleted);

        if (!medicationExists)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Medication not found",
                Detail = $"Medication with ID {medicationId} does not exist or you don't have access."
            });
        }

        // Find active pattern (EndDate = NULL)
        var activePattern = await _context.MedicationDosagePatterns
            .Where(p => p.MedicationId == medicationId && p.EndDate == null)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();

        if (activePattern == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "No active pattern",
                Detail = $"No active dosage pattern found for medication {medicationId}. The medication may be using a fixed dosage."
            });
        }

        var response = MapToResponse(activePattern);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves dosage pattern history for a medication.
    /// Returns patterns ordered by start date (newest first).
    /// </summary>
    /// <param name="medicationId">ID of the medication</param>
    /// <param name="activeOnly">If true, returns only patterns where EndDate is null</param>
    /// <param name="limit">Number of patterns to return (default: 10, max: 100)</param>
    /// <param name="offset">Pagination offset (default: 0)</param>
    /// <returns>List of dosage patterns</returns>
    /// <response code="200">Pattern history retrieved</response>
    /// <response code="403">User doesn't have access to this medication</response>
    /// <response code="404">Medication not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(PatternHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatternHistoryResponse>> GetPatternHistory(
        int medicationId,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int limit = 10,
        [FromQuery] int offset = 0)
    {
        var userPublicId = GetCurrentUserPublicId();
        if (userPublicId == null)
        {
            _logger.LogWarning("Attempted to get pattern history with invalid user ID");
            return Unauthorized();
        }

        // Verify medication exists and user has access
        var medicationExists = await _context.Medications
            .Include(m => m.User)
            .AnyAsync(m => m.Id == medicationId && m.User.PublicId == userPublicId.Value && !m.IsDeleted);

        if (!medicationExists)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Medication not found",
                Detail = $"Medication with ID {medicationId} does not exist or you don't have access."
            });
        }

        // Validate pagination parameters
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(offset, 0);

        // Build query
        var query = _context.MedicationDosagePatterns
            .Where(p => p.MedicationId == medicationId);

        if (activeOnly)
        {
            query = query.Where(p => p.EndDate == null);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Get patterns
        var patterns = await query
            .OrderByDescending(p => p.StartDate)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        var response = new PatternHistoryResponse
        {
            MedicationId = medicationId,
            TotalCount = totalCount,
            Limit = limit,
            Offset = offset,
            Patterns = patterns.Select(MapToResponse).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Maps a MedicationDosagePattern entity to a DosagePatternResponse DTO.
    /// </summary>
    private static DosagePatternResponse MapToResponse(MedicationDosagePattern pattern)
    {
        return new DosagePatternResponse
        {
            Id = pattern.Id,
            MedicationId = pattern.MedicationId,
            PatternSequence = pattern.PatternSequence,
            PatternLength = pattern.PatternLength,
            StartDate = pattern.StartDate,
            EndDate = pattern.EndDate,
            Notes = pattern.Notes,
            IsActive = pattern.IsActive,
            AverageDosage = pattern.AverageDosage,
            DisplayPattern = pattern.GetDisplayPattern(),
            CreatedAt = pattern.CreatedAt,
            UpdatedAt = pattern.UpdatedAt
        };
    }

    /// <summary>
    /// Gets the current user's public GUID from JWT claims.
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
/// Response containing pattern history with pagination metadata.
/// </summary>
public class PatternHistoryResponse
{
    public int MedicationId { get; set; }
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public List<DosagePatternResponse> Patterns { get; set; } = new();
}
