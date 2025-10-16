// BloodThinnerTracker.Api - Authentication Controller for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BloodThinnerTracker.Shared.Models.Authentication;
using BloodThinnerTracker.Api.Services.Authentication;

namespace BloodThinnerTracker.Api.Controllers;

/// <summary>
/// Authentication controller for medical application security
/// Provides secure login, token management, and medical data access control
/// 
/// MEDICAL DISCLAIMER: This system is for medication tracking purposes only.
/// Always consult healthcare professionals for medical decisions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initialize authentication controller
    /// </summary>
    public AuthController(
        IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with JWT tokens</returns>
    /// <response code="200">Login successful, returns JWT tokens and user info</response>
    /// <response code="400">Invalid request format</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="429">Too many login attempts</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login request format for {Email}", request.Email);
                return BadRequest(ModelState);
            }

            var response = await _authenticationService.AuthenticateAsync(request);
            if (response == null)
            {
                _logger.LogWarning("Authentication failed for user {Email}", request.Email);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = "Invalid email or password",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            _logger.LogInformation("User {UserId} logged in successfully", response.User.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during authentication",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response with refreshed tokens</returns>
    /// <response code="200">Token refresh successful</response>
    /// <response code="400">Invalid request format</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid refresh token request format");
                return BadRequest(ModelState);
            }

            var response = await _authenticationService.RefreshTokenAsync(request);
            if (response == null)
            {
                _logger.LogWarning("Token refresh failed for refresh token");
                return Unauthorized(new ProblemDetails
                {
                    Title = "Token Refresh Failed",
                    Detail = "Invalid or expired refresh token",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            _logger.LogInformation("Token refreshed successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during token refresh",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Revoke refresh token to logout user securely
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <returns>Logout confirmation</returns>
    /// <response code="200">Token revoked successfully</response>
    /// <response code="400">Invalid request format</response>
    /// <response code="401">Unauthorized - must be authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout([FromBody] string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Logout attempt with empty refresh token");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Refresh token is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _authenticationService.RevokeTokenAsync(refreshToken);

            if (success)
            {
                _logger.LogInformation("User {UserId} logged out successfully", userId);
                return Ok(new { message = "Logged out successfully" });
            }

            _logger.LogWarning("Failed to revoke refresh token for user {UserId}", userId);
            return BadRequest(new ProblemDetails
            {
                Title = "Logout Failed",
                Detail = "Failed to revoke refresh token",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during logout",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get current user information and permissions
    /// </summary>
    /// <returns>Current user information</returns>
    /// <response code="200">User information retrieved successfully</response>
    /// <response code="401">Unauthorized - must be authenticated</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unable to extract user ID from token");
                return Unauthorized();
            }

            var userInfo = new UserInfo
            {
                Id = userId,
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                Name = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Patient",
                Provider = User.FindFirst("provider")?.Value ?? "Unknown",
                TimeZone = User.FindFirst("timezone")?.Value ?? "UTC"
            };

            var permissions = await _authenticationService.GetUserPermissionsAsync(userId);

            _logger.LogDebug("Retrieved user information for {UserId}", userId);
            return Ok(new { user = userInfo, permissions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting current user");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred retrieving user information",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Validate current session for medical security
    /// </summary>
    /// <returns>Session validation result</returns>
    /// <response code="200">Session is valid</response>
    /// <response code="401">Session is invalid or expired</response>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ValidateSession()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Extract session ID from claims or generate one for validation
            var sessionId = User.FindFirst("session_id")?.Value ?? Guid.NewGuid().ToString();
            var isValid = await _authenticationService.ValidateSessionAsync(userId, sessionId);

            if (isValid)
            {
                _logger.LogDebug("Session validated for user {UserId}", userId);
                return Ok(new { valid = true, userId, timestamp = DateTime.UtcNow });
            }

            _logger.LogWarning("Invalid session for user {UserId}", userId);
            return Unauthorized(new ProblemDetails
            {
                Title = "Session Invalid",
                Detail = "Current session is invalid or expired",
                Status = StatusCodes.Status401Unauthorized
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during session validation");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during session validation",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Health check endpoint for authentication service
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            service = "Authentication",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}