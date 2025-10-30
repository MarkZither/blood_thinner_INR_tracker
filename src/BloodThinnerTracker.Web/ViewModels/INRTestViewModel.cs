using System.ComponentModel.DataAnnotations;

namespace BloodThinnerTracker.Web.ViewModels;

/// <summary>
/// View model for INR test add/edit forms with validation
/// </summary>
public class INRTestViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Test date is required")]
    public DateTime? TestDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Test time is required")]
    public TimeSpan? TestTime { get; set; } = DateTime.Now.TimeOfDay;

    [Required(ErrorMessage = "INR value is required")]
    [Range(0.5, 8.0, ErrorMessage = "INR value must be between 0.5 and 8.0")]
    public decimal? InrValue { get; set; }

    [Range(0.5, 8.0, ErrorMessage = "Target minimum must be between 0.5 and 8.0")]
    public decimal? TargetINRMin { get; set; } = 2.0m;

    [Range(0.5, 8.0, ErrorMessage = "Target maximum must be between 0.5 and 8.0")]
    public decimal? TargetINRMax { get; set; } = 3.0m;

    [StringLength(200, ErrorMessage = "Location must be less than 200 characters")]
    public string? TestLocation { get; set; }

    [StringLength(1000, ErrorMessage = "Notes must be less than 1000 characters")]
    public string? Notes { get; set; }

    public decimal? DosageAdjustment { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Gets the combined date and time for the test
    /// </summary>
    public DateTime GetTestDateTime()
    {
        if (TestDate.HasValue && TestTime.HasValue)
        {
            return TestDate.Value.Date.Add(TestTime.Value);
        }
        return DateTime.Now;
    }

    /// <summary>
    /// Validates that test date is not in future and not more than 1 year old
    /// </summary>
    public bool IsValidTestDate(out string? errorMessage)
    {
        errorMessage = null;
        var testDateTime = GetTestDateTime();

        if (testDateTime > DateTime.Now)
        {
            errorMessage = "Test date cannot be in the future";
            return false;
        }

        if (testDateTime < DateTime.Now.AddYears(-1))
        {
            errorMessage = "Test date cannot be more than 1 year old";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that target range is valid
    /// </summary>
    public bool IsValidTargetRange(out string? errorMessage)
    {
        errorMessage = null;

        if (TargetINRMin.HasValue && TargetINRMax.HasValue)
        {
            if (TargetINRMax <= TargetINRMin)
            {
                errorMessage = "Target maximum must be greater than target minimum";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if INR value is in critical range (< 1.5 or > 4.0)
    /// </summary>
    public bool IsCriticalValue()
    {
        return InrValue.HasValue && (InrValue < 1.5m || InrValue > 4.0m);
    }

    /// <summary>
    /// Checks if INR value is in target range
    /// </summary>
    public bool IsInTargetRange()
    {
        if (!InrValue.HasValue || !TargetINRMin.HasValue || !TargetINRMax.HasValue)
            return false;

        return InrValue >= TargetINRMin && InrValue <= TargetINRMax;
    }

    /// <summary>
    /// Gets the critical value warning message
    /// </summary>
    public string GetCriticalValueWarning()
    {
        if (!InrValue.HasValue) return string.Empty;

        if (InrValue < 1.5m)
            return $"⚠️ Critical: INR {InrValue} is dangerously low. Contact your healthcare provider immediately.";

        if (InrValue > 4.0m)
            return $"⚠️ Critical: INR {InrValue} is dangerously high. Contact your healthcare provider immediately.";

        return string.Empty;
    }
}
