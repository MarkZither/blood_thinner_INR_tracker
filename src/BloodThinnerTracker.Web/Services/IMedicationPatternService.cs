namespace BloodThinnerTracker.Web.Services;

using BloodThinnerTracker.Shared.Models;

/// <summary>
/// Service interface for managing medication dosage patterns via API.
/// </summary>
/// <remarks>
/// This service provides CRUD operations for dosage patterns, supporting:
/// - Creating new patterns with temporal validity
/// - Retrieving active patterns
/// - Fetching pattern history with pagination
/// - Managing pattern transitions (closing previous patterns)
///
/// All operations enforce user authentication and data isolation via JWT tokens.
/// </remarks>
public interface IMedicationPatternService
{
    /// <summary>
    /// Creates a new dosage pattern for a medication.
    /// </summary>
    /// <param name="medicationPublicId">Public ID (GUID) of the medication to add pattern to</param>
    /// <param name="request">Pattern details including sequence, dates, and options</param>
    /// <returns>The created dosage pattern with computed fields</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated</exception>
    Task<DosagePatternResponse?> CreatePatternAsync(
        Guid medicationPublicId,
        CreateDosagePatternRequest request);

    /// <summary>
    /// Retrieves the currently active dosage pattern for a medication.
    /// </summary>
    /// <param name="medicationPublicId">Public ID (GUID) of the medication</param>
    /// <returns>The active pattern, or null if no active pattern exists</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails</exception>
    Task<DosagePatternResponse?> GetActivePatternAsync(Guid medicationPublicId);

    /// <summary>
    /// Retrieves dosage pattern history for a medication.
    /// </summary>
    /// <param name="medicationPublicId">Public ID (GUID) of the medication</param>
    /// <param name="activeOnly">If true, returns only active patterns (EndDate = null)</param>
    /// <param name="limit">Number of patterns to return (default: 10, max: 100)</param>
    /// <param name="offset">Pagination offset (default: 0)</param>
    /// <returns>Pattern history with pagination metadata</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails</exception>
    Task<PatternHistoryResponse?> GetPatternHistoryAsync(
        Guid medicationPublicId,
        bool activeOnly = false,
        int limit = 10,
        int offset = 0);
}

/// <summary>
/// Response containing pattern history with pagination metadata.
/// Matches the API response from GET /api/medications/{id}/patterns
/// </summary>
public class PatternHistoryResponse
{
    public int MedicationId { get; set; }
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public List<DosagePatternResponse> Patterns { get; set; } = new();
}
