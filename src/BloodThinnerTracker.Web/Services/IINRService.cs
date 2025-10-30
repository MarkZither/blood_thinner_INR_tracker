using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Web.Services;

/// <summary>
/// Service interface for INR test management
/// </summary>
public interface IINRService
{
    /// <summary>
    /// Gets all INR tests for the current user
    /// </summary>
    Task<List<INRTest>> GetTestsAsync(DateTime? startDate = null, DateTime? endDate = null, bool? inRange = null);

    /// <summary>
    /// Gets a specific INR test by ID
    /// </summary>
    Task<INRTest?> GetTestByIdAsync(Guid testId);

    /// <summary>
    /// Creates a new INR test
    /// </summary>
    Task<INRTest> CreateTestAsync(INRTest test);

    /// <summary>
    /// Updates an existing INR test
    /// </summary>
    Task<INRTest> UpdateTestAsync(INRTest test);

    /// <summary>
    /// Deletes an INR test
    /// </summary>
    Task DeleteTestAsync(Guid testId);

    /// <summary>
    /// Gets the last INR test for trend comparison
    /// </summary>
    Task<INRTest?> GetLastTestAsync();
}
