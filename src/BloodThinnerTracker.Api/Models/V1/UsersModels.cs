using System.ComponentModel.DataAnnotations;

namespace BloodThinnerTracker.Api.Models.V1;

public sealed class UserProfileResponse
{
    public Guid Id { get; set; }
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

public sealed class NotificationPreferencesResponse
{
    public bool IsEmailNotificationsEnabled { get; set; } = true;
    public bool IsSmsNotificationsEnabled { get; set; } = false;
    public bool IsPushNotificationsEnabled { get; set; } = true;
    public int ReminderAdvanceMinutes { get; set; } = 15;
    public string PreferredLanguage { get; set; } = "en-US";
    public string TimeZone { get; set; } = "UTC";
}

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

public sealed class DeactivateAccountRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    public bool ConfirmDataDeletion { get; set; }
}

public sealed class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}
