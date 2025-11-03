/*
 * BloodThinnerTracker.Api - Users Controller
 * Licensed under MIT License. See LICENSE file in the project root.
 *
 * REST API controller for user profile management in the blood thinner tracking system.
 * Provides endpoints for user registration, profile management, and account settings.
 *
 * ⚠️ MEDICAL DATA CONTROLLER:
 * This controller handles protected health information (PHI). All operations
 * must comply with healthcare data protection regulations and include proper
 * authentication, authorization, and audit logging.
 *
 * IMPORTANT MEDICAL DISCLAIMER:
 * This software is for informational purposes only and should not replace
 * professional medical advice. Users should consult healthcare providers
 * for medical decisions.
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
/// REST API controller for user profile management.
/// Handles user registration, profile updates, preferences, and account management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    /// <summary>
    /// Initializes a new instance of the UsersController.
    /// </summary>
    /// <param name="context">Database context for user data access.</param>
    /// <param name="logger">Logger for operation tracking and debugging.</param>
    public UsersController(IApplicationDbContext context, ILogger<UsersController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current user's profile information.
    /// </summary>
    /// <returns>Current user's profile data.</returns>
    /// <response code="200">User profile retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User profile not found.</response>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetCurrentUserProfile()
    {
        try
        {
            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to get profile with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var user = await _context.Users
                .Where(u => u.PublicId == userPublicId.Value && !u.IsDeleted)
                .Select(u => new UserProfileResponse
                {
                    Id = u.PublicId.ToString(), // API returns public GUID
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DateOfBirth = u.DateOfBirth,
                    PhoneNumber = u.PhoneNumber,
                    EmergencyContact = u.EmergencyContactName,
                    EmergencyPhone = u.EmergencyContactPhone,
                    PreferredLanguage = u.PreferredLanguage,
                    TimeZone = u.TimeZone,
                    IsEmailNotificationsEnabled = u.IsEmailNotificationsEnabled,
                    IsSmsNotificationsEnabled = u.IsSmsNotificationsEnabled,
                    IsPushNotificationsEnabled = u.IsPushNotificationsEnabled,
                    ReminderAdvanceMinutes = u.ReminderAdvanceMinutes,
                    ProfileCompletedAt = u.ProfileCompletedAt,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User profile not found for user PublicId: {UserPublicId}", userPublicId);
                return NotFound("User profile not found");
            }

            _logger.LogInformation("User profile retrieved successfully for user PublicId: {UserPublicId}", userPublicId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile for user PublicId: {UserPublicId}", GetCurrentUserPublicId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving user profile");
        }
    }

    /// <summary>
    /// Updates the current user's profile information.
    /// </summary>
    /// <param name="request">Updated profile information.</param>
    /// <returns>Updated user profile data.</returns>
    /// <response code="200">Profile updated successfully.</response>
    /// <response code="400">Invalid profile data provided.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User profile not found.</response>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> UpdateCurrentUserProfile([FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for profile update: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to update profile with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PublicId == userPublicId.Value && !u.IsDeleted);

            if (user == null)
            {
                _logger.LogWarning("User not found for profile update: {UserPublicId}", userPublicId);
                return NotFound("User not found");
            }

            // Update profile fields
            user.FirstName = request.FirstName?.Trim();
            user.LastName = request.LastName?.Trim();
            user.PhoneNumber = request.PhoneNumber?.Trim();
            user.EmergencyContactName = request.EmergencyContact?.Trim();
            user.EmergencyContactPhone = request.EmergencyPhone?.Trim();
            user.PreferredLanguage = request.PreferredLanguage ?? "en-US";
            user.TimeZone = request.TimeZone ?? "UTC";
            user.IsEmailNotificationsEnabled = request.IsEmailNotificationsEnabled ?? true;
            user.IsSmsNotificationsEnabled = request.IsSmsNotificationsEnabled ?? false;
            user.IsPushNotificationsEnabled = request.IsPushNotificationsEnabled ?? true;
            user.ReminderAdvanceMinutes = Math.Max(5, Math.Min(120, request.ReminderAdvanceMinutes ?? 15));

            // Mark profile as completed if not already
            if (user.ProfileCompletedAt == null &&
                !string.IsNullOrWhiteSpace(user.FirstName) &&
                !string.IsNullOrWhiteSpace(user.LastName))
            {
                user.ProfileCompletedAt = DateTime.UtcNow;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new UserProfileResponse
            {
                Id = user.PublicId.ToString(), // API returns public GUID
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                PhoneNumber = user.PhoneNumber,
                EmergencyContact = user.EmergencyContactName,
                EmergencyPhone = user.EmergencyContactPhone,
                PreferredLanguage = user.PreferredLanguage,
                TimeZone = user.TimeZone,
                IsEmailNotificationsEnabled = user.IsEmailNotificationsEnabled,
                IsSmsNotificationsEnabled = user.IsSmsNotificationsEnabled,
                IsPushNotificationsEnabled = user.IsPushNotificationsEnabled,
                ReminderAdvanceMinutes = user.ReminderAdvanceMinutes,
                ProfileCompletedAt = user.ProfileCompletedAt,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            _logger.LogInformation("User profile updated successfully for user PublicId: {UserPublicId}", userPublicId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for user PublicId: {UserPublicId}", GetCurrentUserPublicId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while updating user profile");
        }
    }

    /// <summary>
    /// Gets the current user's notification preferences.
    /// </summary>
    /// <returns>Current user's notification settings.</returns>
    /// <response code="200">Notification preferences retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationPreferencesResponse>> GetNotificationPreferences()
    {
        try
        {
            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to get notification preferences with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var user = await _context.Users
                .Where(u => u.PublicId == userPublicId.Value && !u.IsDeleted)
                .Select(u => new NotificationPreferencesResponse
                {
                    IsEmailNotificationsEnabled = u.IsEmailNotificationsEnabled,
                    IsSmsNotificationsEnabled = u.IsSmsNotificationsEnabled,
                    IsPushNotificationsEnabled = u.IsPushNotificationsEnabled,
                    ReminderAdvanceMinutes = u.ReminderAdvanceMinutes,
                    PreferredLanguage = u.PreferredLanguage,
                    TimeZone = u.TimeZone
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User not found for notification preferences: {UserPublicId}", userPublicId);
                return NotFound("User not found");
            }

            _logger.LogInformation("Notification preferences retrieved successfully for user PublicId: {UserPublicId}", userPublicId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification preferences for user PublicId: {UserPublicId}", GetCurrentUserPublicId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving notification preferences");
        }
    }

    /// <summary>
    /// Updates the current user's notification preferences.
    /// </summary>
    /// <param name="request">Updated notification preferences.</param>
    /// <returns>Updated notification preferences.</returns>
    /// <response code="200">Notification preferences updated successfully.</response>
    /// <response code="400">Invalid notification preferences provided.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("notifications")]
    [ProducesResponseType(typeof(NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationPreferencesResponse>> UpdateNotificationPreferences([FromBody] UpdateNotificationPreferencesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for notification preferences update: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to update notification preferences with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PublicId == userPublicId.Value && !u.IsDeleted);

            if (user == null)
            {
                _logger.LogWarning("User not found for notification preferences update: {UserPublicId}", userPublicId);
                return NotFound("User not found");
            }

            // Update notification preferences
            user.IsEmailNotificationsEnabled = request.IsEmailNotificationsEnabled ?? user.IsEmailNotificationsEnabled;
            user.IsSmsNotificationsEnabled = request.IsSmsNotificationsEnabled ?? user.IsSmsNotificationsEnabled;
            user.IsPushNotificationsEnabled = request.IsPushNotificationsEnabled ?? user.IsPushNotificationsEnabled;

            // Validate and set reminder advance minutes (5-120 minutes)
            if (request.ReminderAdvanceMinutes.HasValue)
            {
                user.ReminderAdvanceMinutes = Math.Max(5, Math.Min(120, request.ReminderAdvanceMinutes.Value));
            }

            if (!string.IsNullOrWhiteSpace(request.PreferredLanguage))
            {
                user.PreferredLanguage = request.PreferredLanguage;
            }

            if (!string.IsNullOrWhiteSpace(request.TimeZone))
            {
                user.TimeZone = request.TimeZone;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new NotificationPreferencesResponse
            {
                IsEmailNotificationsEnabled = user.IsEmailNotificationsEnabled,
                IsSmsNotificationsEnabled = user.IsSmsNotificationsEnabled,
                IsPushNotificationsEnabled = user.IsPushNotificationsEnabled,
                ReminderAdvanceMinutes = user.ReminderAdvanceMinutes,
                PreferredLanguage = user.PreferredLanguage,
                TimeZone = user.TimeZone
            };

            _logger.LogInformation("Notification preferences updated successfully for user PublicId: {UserPublicId}", userPublicId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for user PublicId: {UserPublicId}", GetCurrentUserPublicId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while updating notification preferences");
        }
    }

    /// <summary>
    /// Deactivates the current user's account (soft delete).
    /// </summary>
    /// <param name="request">Account deactivation request with reason.</param>
    /// <returns>Confirmation of account deactivation.</returns>
    /// <response code="200">Account deactivated successfully.</response>
    /// <response code="400">Invalid deactivation request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("deactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeactivateAccount([FromBody] DeactivateAccountRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for account deactivation: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to deactivate account with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PublicId == userPublicId.Value && !u.IsDeleted);

            if (user == null)
            {
                _logger.LogWarning("User not found for account deactivation: {UserPublicId}", userPublicId);
                return NotFound("User not found");
            }

            // Soft delete the user account
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning("User account deactivated: {UserPublicId}, Reason: {Reason}", userPublicId, request.Reason);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Account has been deactivated successfully. Your medical data has been safely archived.",
                Data = new { DeactivatedAt = user.DeletedAt }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating account for user PublicId: {UserPublicId}", GetCurrentUserPublicId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while deactivating the account");
        }
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
/// Response model for user profile data.
/// </summary>
public sealed class UserProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string PreferredLanguage { get; set; } = "en-US";
    public string TimeZone { get; set; } = "UTC";
    public bool IsEmailNotificationsEnabled { get; set; } = true;
    public bool IsSmsNotificationsEnabled { get; set; } = false;
    public bool IsPushNotificationsEnabled { get; set; } = true;
    public int ReminderAdvanceMinutes { get; set; } = 15;
    public DateTime? ProfileCompletedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request model for updating user profile.
/// </summary>
public sealed class UpdateUserProfileRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? FirstName { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? LastName { get; set; }

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    public string? EmergencyContact { get; set; }

    [Phone]
    [StringLength(20)]
    public string? EmergencyPhone { get; set; }

    [StringLength(10)]
    public string? PreferredLanguage { get; set; }

    [StringLength(50)]
    public string? TimeZone { get; set; }

    public bool? IsEmailNotificationsEnabled { get; set; }

    public bool? IsSmsNotificationsEnabled { get; set; }

    public bool? IsPushNotificationsEnabled { get; set; }

    [Range(5, 120)]
    public int? ReminderAdvanceMinutes { get; set; }
}

/// <summary>
/// Response model for notification preferences.
/// </summary>
public sealed class NotificationPreferencesResponse
{
    public bool IsEmailNotificationsEnabled { get; set; } = true;
    public bool IsSmsNotificationsEnabled { get; set; } = false;
    public bool IsPushNotificationsEnabled { get; set; } = true;
    public int ReminderAdvanceMinutes { get; set; } = 15;
    public string PreferredLanguage { get; set; } = "en-US";
    public string TimeZone { get; set; } = "UTC";
}

/// <summary>
/// Request model for updating notification preferences.
/// </summary>
public sealed class UpdateNotificationPreferencesRequest
{
    public bool? IsEmailNotificationsEnabled { get; set; }

    public bool? IsSmsNotificationsEnabled { get; set; }

    public bool? IsPushNotificationsEnabled { get; set; }

    [Range(5, 120)]
    public int? ReminderAdvanceMinutes { get; set; }

    [StringLength(10)]
    public string? PreferredLanguage { get; set; }

    [StringLength(50)]
    public string? TimeZone { get; set; }
}

/// <summary>
/// Request model for account deactivation.
/// </summary>
public sealed class DeactivateAccountRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    public bool ConfirmDataDeletion { get; set; }
}

/// <summary>
/// Generic API response model.
/// </summary>
public sealed class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}
