using System;

namespace BloodThinnerTracker.Mobile.Models
{
    public class INRTest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? UserId { get; set; }
        public decimal Value { get; set; }
        public string Units { get; set; } = "INR";
        public DateTimeOffset CollectedAt { get; set; }
        public DateTimeOffset? ReportedAt { get; set; }
        public string? Notes { get; set; }
    }
}
