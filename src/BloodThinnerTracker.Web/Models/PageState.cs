namespace BloodThinnerTracker.Web.Models;

/// <summary>
/// Model for storing page state including pagination and scroll position
/// </summary>
public class PageState
{
    /// <summary>
    /// Number of items to display per page
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Scroll position in pixels from the top of the page
    /// </summary>
    public int ScrollPosition { get; set; }

    /// <summary>
    /// Additional filter or sort state (optional, page-specific)
    /// </summary>
    public Dictionary<string, string>? AdditionalState { get; set; }
}
