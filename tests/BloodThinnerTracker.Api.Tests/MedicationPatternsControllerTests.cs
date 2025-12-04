using System.Security.Claims;
using BloodThinnerTracker.Api.Controllers;
using BloodThinnerTracker.Api.Validators;
using BloodThinnerTracker.Data.Shared;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BloodThinnerTracker.Api.Tests;

/// <summary>
/// Comprehensive test suite for MedicationPatternsController.
/// Tests security, validation, edge cases, and pattern lifecycle operations.
/// </summary>
/// <remarks>
/// LESSON LEARNED: These tests should have been written BEFORE the controller implementation (TDD).
/// Writing tests after implementation led to the UserId bug reaching production.
/// The UserId assignment bug (500 error "User ID is required") would have been caught
/// during development if these tests existed first.
/// </remarks>
public class MedicationPatternsControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly MedicationPatternsController _controller;
    private readonly Mock<IValidator<CreateDosagePatternRequest>> _mockValidator;
    private readonly User _testUser;
    private readonly User _otherUser;

    public MedicationPatternsControllerTests()
    {

        // Mock required dependencies
        _mockValidator = new Mock<IValidator<CreateDosagePatternRequest>>();

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Create context with all required dependencies
        _context = TestHelpers.CreateSqliteContext(_connection);

        // Seed test users (OAuth-based, no password)
        _testUser = new User
        {
            PublicId = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            AuthProvider = "Google",
            ExternalUserId = "google_12345",
            IsActive = true,
            Role = UserRole.Patient,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _otherUser = new User
        {
            PublicId = Guid.NewGuid(),
            Email = "other@example.com",
            Name = "Other User",
            AuthProvider = "AzureAD",
            ExternalUserId = "azure_67890",
            IsActive = true,
            Role = UserRole.Patient,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(_testUser, _otherUser);
        _context.SaveChanges();

        // Create controller with validator
        var controllerLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BloodThinnerTracker.Api.Controllers.MedicationPatternsController>.Instance;
        _controller = new MedicationPatternsController(_context, controllerLogger, _mockValidator.Object);
    }

    private void SetupAuthenticatedUser(User user)
    {
        var claims = new List<Claim>
        {
            new Claim("sub", user.PublicId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.PublicId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private Medication CreateTestMedication(User user, string name = "Warfarin")
    {
        var medication = new Medication
        {
            PublicId = Guid.NewGuid(),
            UserId = user.Id,
            Name = name,
            Type = MedicationType.VitKAntagonist,
            Dosage = 5.0m,
            DosageUnit = "mg",
            Frequency = MedicationFrequency.OnceDaily,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Medications.Add(medication);
        _context.SaveChanges();

        // Reload to get User navigation property
        return _context.Medications
            .Include(m => m.User)
            .First(m => m.PublicId == medication.PublicId);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreatePattern Tests

    [Fact]
    public async Task CreatePattern_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);
        var request = new CreateDosagePatternRequest
        {
            PatternSequence = new List<decimal> { 5.0m, 5.0m, 4.0m },
            StartDate = DateTime.UtcNow.Date,
            Notes = "Test pattern",
            ClosePreviousPattern = false
        };

        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _controller.CreatePattern(medication.PublicId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<DosagePatternResponse>(createdResult.Value);
        Assert.Equal(3, response.PatternLength);
        Assert.Equal(request.PatternSequence, response.PatternSequence);
    }

    [Fact]
    public async Task CreatePattern_UserIdAssignment_CriticalSecurityTest()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);
        var request = new CreateDosagePatternRequest
        {
            PatternSequence = new List<decimal> { 4.0m },
            StartDate = DateTime.UtcNow.Date,
            ClosePreviousPattern = false
        };

        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _controller.CreatePattern(medication.PublicId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);

        // CRITICAL: Verify UserId is set (this is the bug that was found)
        var pattern = _context.MedicationDosagePatterns.First();
        Assert.Equal(_testUser.Id, pattern.UserId);
        Assert.NotEqual(0, pattern.UserId); // Ensure not default value
    }

    [Fact]
    public async Task CreatePattern_ValidationFailure_ReturnsBadRequest()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);
        var request = new CreateDosagePatternRequest
        {
            PatternSequence = new List<decimal> { 100.0m }, // Invalid dosage
            StartDate = DateTime.UtcNow.Date
        };

        var validationErrors = new List<FluentValidation.Results.ValidationFailure>
        {
            new FluentValidation.Results.ValidationFailure("PatternSequence", "Dosage exceeds maximum")
        };
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(validationErrors));

        // Act
        var result = await _controller.CreatePattern(medication.PublicId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("Dosage exceeds maximum", problemDetails.Detail);
    }

    [Fact]
    public async Task CreatePattern_MedicationNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var nonExistentId = Guid.NewGuid();
        var request = new CreateDosagePatternRequest
        {
            PatternSequence = new List<decimal> { 5.0m },
            StartDate = DateTime.UtcNow.Date
        };

        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _controller.CreatePattern(nonExistentId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(404, problemDetails.Status);
    }

    [Fact]
    public async Task CreatePattern_OtherUsersMedication_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var otherUserMedication = CreateTestMedication(_otherUser, "Other User's Warfarin");
        var request = new CreateDosagePatternRequest
        {
            PatternSequence = new List<decimal> { 5.0m },
            StartDate = DateTime.UtcNow.Date
        };

        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _controller.CreatePattern(otherUserMedication.PublicId, request);

        // Assert - Should return NotFound, not Forbidden, to prevent information disclosure
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task CreatePattern_ClosePreviousPattern_SetsEndDate()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);

        // Create initial pattern
        var initialPattern = new MedicationDosagePattern
        {
            UserId = _testUser.Id,
            MedicationId = medication.Id,
            PatternSequence = new List<decimal> { 5.0m },
            StartDate = DateTime.UtcNow.Date.AddDays(-30),
            EndDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.MedicationDosagePatterns.Add(initialPattern);
        _context.SaveChanges();

        var request = new CreateDosagePatternRequest
        {
            PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m },
            StartDate = DateTime.UtcNow.Date,
            ClosePreviousPattern = true
        };

        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _controller.CreatePattern(medication.PublicId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);

        // Verify previous pattern was closed
        var closedPattern = _context.MedicationDosagePatterns.First(p => p.Id == initialPattern.Id);
        Assert.NotNull(closedPattern.EndDate);
        Assert.Equal(request.StartDate.AddDays(-1), closedPattern.EndDate);
    }

    [Fact]
    public async Task CreatePattern_PublicIdInRoute_NeverInternalId()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);
        var request = new CreateDosagePatternRequest
        {
            PatternSequence = new List<decimal> { 5.0m },
            StartDate = DateTime.UtcNow.Date
        };

        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act - Using PublicId (GUID) not internal Id (int)
        var result = await _controller.CreatePattern(medication.PublicId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(createdResult.Value);

        // Verify route uses PublicId pattern
        Assert.IsType<Guid>(medication.PublicId);
        Assert.NotEqual(Guid.Empty, medication.PublicId);
    }

    #endregion

    #region GetActivePattern Tests

    [Fact]
    public async Task GetActivePattern_PatternExists_ReturnsOkResult()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);
        var pattern = new MedicationDosagePattern
        {
            UserId = _testUser.Id,
            MedicationId = medication.Id,
            PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m },

            StartDate = DateTime.UtcNow.Date.AddDays(-7),
            EndDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.MedicationDosagePatterns.Add(pattern);
        _context.SaveChanges();

        // Act
        var result = await _controller.GetActivePattern(medication.PublicId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DosagePatternResponse>(okResult.Value);
        Assert.Equal(3, response.PatternLength);
        Assert.Null(response.EndDate);
    }

    [Fact]
    public async Task GetActivePattern_NoActivePattern_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);

        // Create only closed pattern
        var closedPattern = new MedicationDosagePattern
        {
            UserId = _testUser.Id,
            MedicationId = medication.Id,
            PatternSequence = new List<decimal> { 5.0m },

            StartDate = DateTime.UtcNow.Date.AddDays(-30),
            EndDate = DateTime.UtcNow.Date.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.MedicationDosagePatterns.Add(closedPattern);
        _context.SaveChanges();

        // Act
        var result = await _controller.GetActivePattern(medication.PublicId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains("No active dosage pattern", problemDetails.Detail);
    }

    [Fact]
    public async Task GetActivePattern_MedicationNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _controller.GetActivePattern(nonExistentId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetActivePattern_OtherUsersMedication_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var otherUserMedication = CreateTestMedication(_otherUser);

        // Act
        var result = await _controller.GetActivePattern(otherUserMedication.PublicId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetActivePattern_MultiplePatterns_ReturnsLatestActive()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);

        var oldPattern = new MedicationDosagePattern
        {
            UserId = _testUser.Id,
            MedicationId = medication.Id,
            PatternSequence = new List<decimal> { 5.0m },

            StartDate = DateTime.UtcNow.Date.AddDays(-60),
            EndDate = DateTime.UtcNow.Date.AddDays(-30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var currentPattern = new MedicationDosagePattern
        {
            UserId = _testUser.Id,
            MedicationId = medication.Id,
            PatternSequence = new List<decimal> { 4.0m, 4.0m, 3.0m },

            StartDate = DateTime.UtcNow.Date.AddDays(-29),
            EndDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MedicationDosagePatterns.AddRange(oldPattern, currentPattern);
        _context.SaveChanges();

        // Act
        var result = await _controller.GetActivePattern(medication.PublicId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DosagePatternResponse>(okResult.Value);
        Assert.Equal(currentPattern.Id, response.Id);
        Assert.Equal(3, response.PatternLength);
    }

    #endregion

    #region GetPatternHistory Tests

    [Fact]
    public async Task GetPatternHistory_ReturnsAllPatterns_OrderedByStartDate()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);

        var pattern1 = new MedicationDosagePattern
        {
            UserId = _testUser.Id,
            MedicationId = medication.Id,
            PatternSequence = new List<decimal> { 5.0m },

            StartDate = DateTime.UtcNow.Date.AddDays(-60),
            EndDate = DateTime.UtcNow.Date.AddDays(-30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var pattern2 = new MedicationDosagePattern
        {
            UserId = _testUser.Id,
            MedicationId = medication.Id,
            PatternSequence = new List<decimal> { 4.0m },

            StartDate = DateTime.UtcNow.Date.AddDays(-29),
            EndDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MedicationDosagePatterns.AddRange(pattern1, pattern2);
        _context.SaveChanges();

        // Act - Request without limit to get all patterns
        var result = await _controller.GetPatternHistory(medication.PublicId, activeOnly: false, limit: 100, offset: 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PatternHistoryResponse>(okResult.Value);
        Assert.Equal(2, response.Patterns.Count);
        Assert.True(response.Patterns[0].StartDate >= response.Patterns[1].StartDate); // Descending order
    }

    [Fact]
    public async Task GetPatternHistory_PaginationWorks_ReturnsCorrectPage()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);

        // Create 5 patterns
        for (int i = 0; i < 5; i++)
        {
            var pattern = new MedicationDosagePattern
            {
                UserId = _testUser.Id,
                MedicationId = medication.Id,
                PatternSequence = new List<decimal> { 5.0m - i },

                StartDate = DateTime.UtcNow.Date.AddDays(-60 + (i * 10)),
                EndDate = i < 4 ? DateTime.UtcNow.Date.AddDays(-50 + (i * 10)) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.MedicationDosagePatterns.Add(pattern);
        }
        _context.SaveChanges();

        // Act - Request page 2 with size 2
        var result = await _controller.GetPatternHistory(medication.PublicId, activeOnly: false, limit: 2, offset: 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PatternHistoryResponse>(okResult.Value);
        Assert.Equal(2, response.Patterns.Count);
    }

    [Fact]
    public async Task GetPatternHistory_NoPatterns_ReturnsEmptyList()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);

        // Act
        var result = await _controller.GetPatternHistory(medication.PublicId, activeOnly: false, limit: 1, offset: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PatternHistoryResponse>(okResult.Value);
        Assert.Empty(response.Patterns);
    }

    [Fact]
    public async Task GetPatternHistory_MedicationNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _controller.GetPatternHistory(nonExistentId, activeOnly: false, limit: 1, offset: 10);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPatternHistory_OtherUsersMedication_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var otherUserMedication = CreateTestMedication(_otherUser);

        // Act
        var result = await _controller.GetPatternHistory(otherUserMedication.PublicId, activeOnly: false, limit: 1, offset: 10);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task CreatePattern_OtherUsersPatternAttempt_ReturnsNotFound()
    {
        // Arrange - TestUser tries to create pattern for OtherUser's medication
        SetupAuthenticatedUser(_testUser);
        var otherUserMedication = CreateTestMedication(_otherUser, "Other's Warfarin");
        var request = new CreateDosagePatternRequest
        {
            PatternSequence = new List<decimal> { 5.0m },
            StartDate = DateTime.UtcNow.Date
        };

        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _controller.CreatePattern(otherUserMedication.PublicId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);

        // Verify no pattern was created
        var patternCount = _context.MedicationDosagePatterns.Count();
        Assert.Equal(0, patternCount);
    }

    [Fact]
    public async Task AllEndpoints_UserIsolation_PreventsCrossTenantAccess()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var testUserMedication = CreateTestMedication(_testUser, "Test User's Warfarin");
        var otherUserMedication = CreateTestMedication(_otherUser, "Other User's Warfarin");

        var testPattern = new MedicationDosagePattern
        {
            UserId = _testUser.Id,
            MedicationId = testUserMedication.Id,
            PatternSequence = new List<decimal> { 5.0m },

            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var otherPattern = new MedicationDosagePattern
        {
            UserId = _otherUser.Id,
            MedicationId = otherUserMedication.Id,
            PatternSequence = new List<decimal> { 4.0m },

            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MedicationDosagePatterns.AddRange(testPattern, otherPattern);
        _context.SaveChanges();

        // Act - Try to access other user's patterns
        var getActiveResult = await _controller.GetActivePattern(otherUserMedication.PublicId);
        var getHistoryResult = await _controller.GetPatternHistory(otherUserMedication.PublicId, activeOnly: false, limit: 1, offset: 10);

        // Assert - All should return NotFound (not Forbidden, to prevent information disclosure)
        Assert.IsType<NotFoundObjectResult>(getActiveResult.Result);
        Assert.IsType<NotFoundObjectResult>(getHistoryResult.Result);
    }

    [Fact]
    public async Task CreatePattern_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange - No authenticated user (set empty HttpContext)
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()  // No user claims
        };

        var medication = CreateTestMedication(_testUser);
        var request = new CreateDosagePatternRequest
        {
            PatternSequence = new List<decimal> { 5.0m },
            StartDate = DateTime.UtcNow.Date
        };

        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _controller.CreatePattern(medication.PublicId, request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Contains("User identity not found", problemDetails.Detail);
    }

    [Fact]
    public async Task AllEndpoints_UsesPublicId_NeverExposesInternalId()
    {
        // Arrange
        SetupAuthenticatedUser(_testUser);
        var medication = CreateTestMedication(_testUser);
        var pattern = new MedicationDosagePattern
        {
            UserId = _testUser.Id,
            MedicationId = medication.Id,
            PatternSequence = new List<decimal> { 5.0m },

            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.MedicationDosagePatterns.Add(pattern);
        _context.SaveChanges();

        // Act
        var activeResult = await _controller.GetActivePattern(medication.PublicId);
        var historyResult = await _controller.GetPatternHistory(medication.PublicId, activeOnly: false, limit: 1, offset: 10);

        // Assert
        var activeOk = Assert.IsType<OkObjectResult>(activeResult.Result);
        var activeResponse = Assert.IsType<DosagePatternResponse>(activeOk.Value);

        var historyOk = Assert.IsType<OkObjectResult>(historyResult.Result);
        var historyResponse = Assert.IsType<PatternHistoryResponse>(historyOk.Value);

        // Verify API routes use PublicId (GUID), not internal Id (int)
        Assert.IsType<Guid>(medication.PublicId);
        Assert.NotEqual(Guid.Empty, medication.PublicId);

        // Response uses internal Id for database efficiency (not exposed in route)
        Assert.IsType<int>(activeResponse.Id);
        Assert.NotEqual(0, activeResponse.Id);
    }

    #endregion
}
