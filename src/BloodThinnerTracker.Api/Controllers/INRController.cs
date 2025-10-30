/*
 * BloodThinnerTracker.Api - INR Tests Controller
 * Licensed under MIT License. See LICENSE file in the project root.
 *
 * REST API controller for INR test management in the blood thinner tracking system.
 * Provides endpoints for logging INR tests, retrieving history, and tracking trends.
 *
 * ⚠️ MEDICAL DATA CONTROLLER:
 * This controller handles protected health information (PHI). All operations
 * must comply with healthcare data protection regulations and include proper
 * authentication, authorization, and audit logging.
 *
 * IMPORTANT MEDICAL DISCLAIMER:
 * This software is for informational purposes only and should not replace
 * professional medical advice. Users should consult healthcare providers
 * for INR interpretation and medication adjustments.
 */

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BloodThinnerTracker.Api.Data;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Controllers;

/// <summary>
/// REST API controller for INR test management.
/// Handles INR test logging, history retrieval, and trend analysis.
/// </summary>
[ApiController]
[Route("api/v1/inr/tests")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public sealed class INRController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<INRController> _logger;

    /// <summary>
    /// Initializes a new instance of the INRController.
    /// </summary>
    /// <param name="context">Database context for INR data access.</param>
    /// <param name="logger">Logger for operation tracking and debugging.</param>
    public INRController(ApplicationDbContext context, ILogger<INRController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves INR tests for the current user with optional filtering.
    /// </summary>
    /// <param name="fromDate">Optional start date for filtering.</param>
    /// <param name="toDate">Optional end date for filtering.</param>
    /// <param name="skip">Number of records to skip (for pagination).</param>
    /// <param name="take">Number of records to return (max 100).</param>
    /// <returns>List of INR test records.</returns>
    /// <response code="200">Returns the list of INR tests.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<INRTestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<INRTestResponse>>> GetINRTests(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user authentication");
            }

            // Limit take to maximum of 100 records
            take = Math.Min(take, 100);

            var query = _context.INRTests
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .AsQueryable();

            // Apply date filters
            if (fromDate.HasValue)
            {
                query = query.Where(t => t.TestDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.TestDate <= toDate.Value);
            }

            // Apply pagination and ordering
            var tests = await query
                .OrderByDescending(t => t.TestDate)
                .Skip(skip)
                .Take(take)
                .Select(t => new INRTestResponse
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    TestDate = t.TestDate,
                    INRValue = t.INRValue,
                    TargetINRMin = t.TargetINRMin,
                    TargetINRMax = t.TargetINRMax,
                    ProthrombinTime = t.ProthrombinTime,
                    PartialThromboplastinTime = t.PartialThromboplastinTime,
                    Laboratory = t.Laboratory,
                    OrderedBy = t.OrderedBy,
                    TestMethod = t.TestMethod,
                    IsPointOfCare = t.IsPointOfCare,
                    WasFasting = t.WasFasting,
                    LastMedicationTime = t.LastMedicationTime,
                    MedicationsTaken = t.MedicationsTaken,
                    FoodsConsumed = t.FoodsConsumed,
                    HealthConditions = t.HealthConditions,
                    Status = t.Status,
                    RecommendedActions = t.RecommendedActions,
                    DosageChanges = t.DosageChanges,
                    NextTestDate = t.NextTestDate,
                    Notes = t.Notes,
                    ReviewedByProvider = t.ReviewedByProvider,
                    ReviewedBy = t.ReviewedBy,
                    ReviewedAt = t.ReviewedAt,
                    PatientNotified = t.PatientNotified,
                    NotificationMethod = t.NotificationMethod,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} INR tests for user {UserId}", tests.Count, userId);
            return Ok(tests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving INR tests");
            return StatusCode(500, "An error occurred while retrieving INR tests");
        }
    }

    /// <summary>
    /// Retrieves a specific INR test by ID.
    /// </summary>
    /// <param name="id">The test ID.</param>
    /// <returns>The INR test record.</returns>
    /// <response code="200">Returns the INR test.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">INR test not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(INRTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<INRTestResponse>> GetINRTest(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user authentication");
            }

            var test = await _context.INRTests
                .Where(t => t.Id == id && t.UserId == userId && !t.IsDeleted)
                .Select(t => new INRTestResponse
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    TestDate = t.TestDate,
                    INRValue = t.INRValue,
                    TargetINRMin = t.TargetINRMin,
                    TargetINRMax = t.TargetINRMax,
                    ProthrombinTime = t.ProthrombinTime,
                    PartialThromboplastinTime = t.PartialThromboplastinTime,
                    Laboratory = t.Laboratory,
                    OrderedBy = t.OrderedBy,
                    TestMethod = t.TestMethod,
                    IsPointOfCare = t.IsPointOfCare,
                    WasFasting = t.WasFasting,
                    LastMedicationTime = t.LastMedicationTime,
                    MedicationsTaken = t.MedicationsTaken,
                    FoodsConsumed = t.FoodsConsumed,
                    HealthConditions = t.HealthConditions,
                    Status = t.Status,
                    RecommendedActions = t.RecommendedActions,
                    DosageChanges = t.DosageChanges,
                    NextTestDate = t.NextTestDate,
                    Notes = t.Notes,
                    ReviewedByProvider = t.ReviewedByProvider,
                    ReviewedBy = t.ReviewedBy,
                    ReviewedAt = t.ReviewedAt,
                    PatientNotified = t.PatientNotified,
                    NotificationMethod = t.NotificationMethod,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (test == null)
            {
                return NotFound($"INR test with ID {id} not found");
            }

            _logger.LogInformation("Retrieved INR test {Id} for user {UserId}", id, userId);
            return Ok(test);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving INR test {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the INR test");
        }
    }

    /// <summary>
    /// Creates a new INR test.
    /// </summary>
    /// <param name="request">INR test data.</param>
    /// <returns>The created INR test.</returns>
    /// <response code="201">INR test created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(INRTestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<INRTestResponse>> CreateINRTest([FromBody] CreateINRTestRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user authentication");
            }

            // Validate INR value range
            if (request.INRValue < 0.5m || request.INRValue > 8.0m)
            {
                return BadRequest("INR value must be between 0.5 and 8.0");
            }

            // Create the test entity
            var test = new INRTest
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                TestDate = request.TestDate,
                INRValue = request.INRValue,
                TargetINRMin = request.TargetINRMin,
                TargetINRMax = request.TargetINRMax,
                ProthrombinTime = request.ProthrombinTime,
                PartialThromboplastinTime = request.PartialThromboplastinTime,
                Laboratory = request.Laboratory,
                OrderedBy = request.OrderedBy,
                TestMethod = request.TestMethod,
                IsPointOfCare = request.IsPointOfCare,
                WasFasting = request.WasFasting,
                LastMedicationTime = request.LastMedicationTime,
                MedicationsTaken = request.MedicationsTaken,
                FoodsConsumed = request.FoodsConsumed,
                HealthConditions = request.HealthConditions,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Set status based on therapeutic range
            test.Status = test.IsInTargetRange()
                ? INRResultStatus.InRange
                : (test.INRValue < (test.TargetINRMin ?? 0)
                    ? INRResultStatus.BelowRange
                    : INRResultStatus.AboveRange);

            _context.INRTests.Add(test);
            await _context.SaveChangesAsync();

            var response = new INRTestResponse
            {
                Id = test.Id,
                UserId = test.UserId,
                TestDate = test.TestDate,
                INRValue = test.INRValue,
                TargetINRMin = test.TargetINRMin,
                TargetINRMax = test.TargetINRMax,
                ProthrombinTime = test.ProthrombinTime,
                PartialThromboplastinTime = test.PartialThromboplastinTime,
                Laboratory = test.Laboratory,
                OrderedBy = test.OrderedBy,
                TestMethod = test.TestMethod,
                IsPointOfCare = test.IsPointOfCare,
                WasFasting = test.WasFasting,
                LastMedicationTime = test.LastMedicationTime,
                MedicationsTaken = test.MedicationsTaken,
                FoodsConsumed = test.FoodsConsumed,
                HealthConditions = test.HealthConditions,
                Status = test.Status,
                RecommendedActions = test.RecommendedActions,
                DosageChanges = test.DosageChanges,
                NextTestDate = test.NextTestDate,
                Notes = test.Notes,
                ReviewedByProvider = test.ReviewedByProvider,
                ReviewedBy = test.ReviewedBy,
                ReviewedAt = test.ReviewedAt,
                PatientNotified = test.PatientNotified,
                NotificationMethod = test.NotificationMethod,
                CreatedAt = test.CreatedAt,
                UpdatedAt = test.UpdatedAt
            };

            _logger.LogInformation("Created INR test {Id} for user {UserId}", test.Id, userId);
            return CreatedAtAction(nameof(GetINRTest), new { id = test.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating INR test");
            return StatusCode(500, "An error occurred while creating the INR test");
        }
    }

    /// <summary>
    /// Updates an existing INR test.
    /// </summary>
    /// <param name="id">The test ID to update.</param>
    /// <param name="request">Updated INR test data.</param>
    /// <returns>The updated INR test.</returns>
    /// <response code="200">INR test updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">INR test not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(INRTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<INRTestResponse>> UpdateINRTest(string id, [FromBody] UpdateINRTestRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user authentication");
            }

            // Validate INR value range
            if (request.INRValue.HasValue && (request.INRValue < 0.5m || request.INRValue > 8.0m))
            {
                return BadRequest("INR value must be between 0.5 and 8.0");
            }

            var test = await _context.INRTests
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && !t.IsDeleted);

            if (test == null)
            {
                return NotFound($"INR test with ID {id} not found");
            }

            // Update the test properties (only if provided)
            if (request.TestDate.HasValue)
                test.TestDate = request.TestDate.Value;
            if (request.INRValue.HasValue)
                test.INRValue = request.INRValue.Value;
            if (request.TargetINRMin.HasValue)
                test.TargetINRMin = request.TargetINRMin;
            if (request.TargetINRMax.HasValue)
                test.TargetINRMax = request.TargetINRMax;
            if (request.ProthrombinTime.HasValue)
                test.ProthrombinTime = request.ProthrombinTime;
            if (request.PartialThromboplastinTime.HasValue)
                test.PartialThromboplastinTime = request.PartialThromboplastinTime;
            if (request.Laboratory != null)
                test.Laboratory = request.Laboratory;
            if (request.OrderedBy != null)
                test.OrderedBy = request.OrderedBy;
            if (request.TestMethod != null)
                test.TestMethod = request.TestMethod;
            if (request.IsPointOfCare.HasValue)
                test.IsPointOfCare = request.IsPointOfCare.Value;
            if (request.WasFasting.HasValue)
                test.WasFasting = request.WasFasting;
            if (request.LastMedicationTime.HasValue)
                test.LastMedicationTime = request.LastMedicationTime;
            if (request.MedicationsTaken != null)
                test.MedicationsTaken = request.MedicationsTaken;
            if (request.FoodsConsumed != null)
                test.FoodsConsumed = request.FoodsConsumed;
            if (request.HealthConditions != null)
                test.HealthConditions = request.HealthConditions;
            if (request.DosageChanges != null)
                test.DosageChanges = request.DosageChanges;
            if (request.Notes != null)
                test.Notes = request.Notes;

            test.UpdatedAt = DateTime.UtcNow;

            // Update status based on therapeutic range
            test.Status = test.IsInTargetRange()
                ? INRResultStatus.InRange
                : (test.INRValue < (test.TargetINRMin ?? 0)
                    ? INRResultStatus.BelowRange
                    : INRResultStatus.AboveRange);

            await _context.SaveChangesAsync();

            var response = new INRTestResponse
            {
                Id = test.Id,
                UserId = test.UserId,
                TestDate = test.TestDate,
                INRValue = test.INRValue,
                TargetINRMin = test.TargetINRMin,
                TargetINRMax = test.TargetINRMax,
                ProthrombinTime = test.ProthrombinTime,
                PartialThromboplastinTime = test.PartialThromboplastinTime,
                Laboratory = test.Laboratory,
                OrderedBy = test.OrderedBy,
                TestMethod = test.TestMethod,
                IsPointOfCare = test.IsPointOfCare,
                WasFasting = test.WasFasting,
                LastMedicationTime = test.LastMedicationTime,
                MedicationsTaken = test.MedicationsTaken,
                FoodsConsumed = test.FoodsConsumed,
                HealthConditions = test.HealthConditions,
                Status = test.Status,
                RecommendedActions = test.RecommendedActions,
                DosageChanges = test.DosageChanges,
                NextTestDate = test.NextTestDate,
                Notes = test.Notes,
                ReviewedByProvider = test.ReviewedByProvider,
                ReviewedBy = test.ReviewedBy,
                ReviewedAt = test.ReviewedAt,
                PatientNotified = test.PatientNotified,
                NotificationMethod = test.NotificationMethod,
                CreatedAt = test.CreatedAt,
                UpdatedAt = test.UpdatedAt
            };

            _logger.LogInformation("Updated INR test {Id} for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating INR test {Id}", id);
            return StatusCode(500, "An error occurred while updating the INR test");
        }
    }

    /// <summary>
    /// Deletes an INR test (soft delete).
    /// </summary>
    /// <param name="id">The test ID to delete.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">INR test deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">INR test not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteINRTest(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user authentication");
            }

            var test = await _context.INRTests
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && !t.IsDeleted);

            if (test == null)
            {
                return NotFound($"INR test with ID {id} not found");
            }

            // Soft delete
            test.IsDeleted = true;
            test.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted INR test {Id} for user {UserId}", id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting INR test {Id}", id);
            return StatusCode(500, "An error occurred while deleting the INR test");
        }
    }

    /// <summary>
    /// Gets the current user ID from the JWT claims.
    /// </summary>
    /// <returns>The user ID or null if not found.</returns>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
