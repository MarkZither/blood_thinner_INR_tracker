namespace BloodThinnerTracker.Api.Controllers;

using BloodThinnerTracker.Data.Shared;
using BloodThinnerTracker.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

/// <summary>
/// API controller for calculating future medication dosage schedules.
/// Provides day-by-day dosage plans based on active patterns.
/// </summary>
/// <remarks>
/// ⚠️ MEDICAL DATA: All endpoints require authentication and enforce user-medication ownership.
/// Schedules are calculated projections - users should verify each dose before taking.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/medications/{medicationPublicId:guid}/schedule")]
[Produces("application/json")]
public class MedicationScheduleController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<MedicationScheduleController> _logger;

    public MedicationScheduleController(
        IApplicationDbContext context,
        ILogger<MedicationScheduleController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculates future dosage schedule for a medication.
    /// </summary>
    /// <param name="medicationPublicId">Public ID (GUID) of the medication</param>
    /// <param name="startDate">Start date for schedule (default: today)</param>
    /// <param name="days">Number of days to calculate (default: 14, max: 365)</param>
    /// <param name="includePatternChanges">Include pattern transition detection (default: true)</param>
    /// <returns>Day-by-day dosage schedule with summary statistics</returns>
    /// <response code="200">Schedule calculated successfully</response>
    /// <response code="400">Invalid parameters (days out of range, invalid date)</response>
    /// <response code="403">User doesn't have access to this medication</response>
    /// <response code="404">Medication not found or no active pattern</response>
    [HttpGet]
    [ProducesResponseType(typeof(MedicationScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationScheduleResponse>> GetSchedule(
        Guid medicationPublicId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] int days = 14,
        [FromQuery] bool includePatternChanges = true)
    {
        var userPublicId = GetCurrentUserPublicId();
        if (userPublicId == null)
        {
            _logger.LogWarning("Attempted to get schedule with invalid user ID");
            return Unauthorized();
        }

        // Validate parameters
        if (days < 1 || days > 365)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid days parameter",
                Detail = "Days must be between 1 and 365"
            });
        }

        var effectiveStartDate = startDate ?? DateTime.Today;
        if (effectiveStartDate < DateTime.Today.AddYears(-1))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid start date",
                Detail = "Start date cannot be more than 1 year in the past"
            });
        }

        // Load medication with patterns
        var medication = await _context.Medications
            .Include(m => m.User)
            .Include(m => m.DosagePatterns.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(m => m.PublicId == medicationPublicId && m.User.PublicId == userPublicId.Value && !m.IsDeleted);

        if (medication == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Medication not found",
                Detail = $"Medication with Public ID {medicationPublicId} does not exist or you don't have access."
            });
        }

        // Calculate schedule (T056)
        var schedule = new List<ScheduleEntry>();
        var endDate = effectiveStartDate.AddDays(days - 1);
        MedicationDosagePattern? previousPattern = null;

        for (int i = 0; i < days; i++)
        {
            var currentDate = effectiveStartDate.AddDays(i);
            var dosage = medication.GetExpectedDosageForDate(currentDate);

            if (dosage == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "No dosage pattern found",
                    Detail = $"This medication does not have an active dosage pattern for {currentDate:yyyy-MM-dd}."
                });
            }

            var currentPattern = medication.GetPatternForDate(currentDate);
            int? patternDay = null;
            int? patternLength = null;

            if (currentPattern != null)
            {
                int daysSinceStart = (currentDate.Date - currentPattern.StartDate.Date).Days;
                patternDay = (daysSinceStart % currentPattern.PatternLength) + 1;
                patternLength = currentPattern.PatternLength;
            }
            else
            {
                // If no pattern (fixed dosage), use day 1 and length 1
                patternDay = 1;
                patternLength = 1;
            }

            // T057: Detect pattern changes
            bool isPatternChange = false;
            string? patternChangeNote = null;

            if (includePatternChanges && currentPattern != null)
            {
                if (previousPattern == null || previousPattern.Id != currentPattern.Id)
                {
                    isPatternChange = true;
                    patternChangeNote = $"New pattern starts: {currentPattern.GetDisplayPattern()}";
                }
            }

            schedule.Add(new ScheduleEntry
            {
                Date = currentDate,
                DayOfWeek = currentDate.ToString("dddd"),
                Dosage = dosage.Value,
                PatternDay = patternDay ?? 1,
                PatternLength = patternLength ?? 1,
                IsPatternChange = isPatternChange,
                PatternChangeNote = patternChangeNote
            });

            previousPattern = currentPattern;
        }

        // T058: Calculate summary statistics
        var dosages = schedule.Select(s => s.Dosage).ToList();
        var activePattern = medication.GetPatternForDate(effectiveStartDate);

        var summary = new ScheduleSummary
        {
            TotalDosage = dosages.Sum(),
            AverageDailyDosage = Math.Round(dosages.Average(), 2),
            MinDosage = dosages.Min(),
            MaxDosage = dosages.Max(),
            PatternCycles = activePattern != null ? Math.Round((decimal)days / activePattern.PatternLength, 2) : 0
        };

        var response = new MedicationScheduleResponse
        {
            MedicationId = medication.Id,
            MedicationName = medication.Name,
            DosageUnit = medication.DosageUnit,
            StartDate = effectiveStartDate,
            EndDate = endDate,
            TotalDays = days,
            CurrentPattern = activePattern != null ? new PatternSummary
            {
                Id = activePattern.Id,
                PatternSequence = activePattern.PatternSequence,
                PatternLength = activePattern.PatternLength,
                StartDate = activePattern.StartDate,
                DisplayPattern = activePattern.GetDisplayPattern()
            } : new PatternSummary
            {
                Id = 0,
                PatternSequence = new List<decimal> { medication.Dosage },
                PatternLength = 1,
                StartDate = effectiveStartDate,
                DisplayPattern = $"{medication.Dosage}{medication.DosageUnit} daily"
            },
            Summary = summary,
            Schedule = schedule
        };

        _logger.LogInformation(
            "Generated {Days}-day schedule for medication {MedicationId}, total dosage: {TotalDosage}",
            days, medication.Id, summary.TotalDosage);

        return Ok(response);
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
