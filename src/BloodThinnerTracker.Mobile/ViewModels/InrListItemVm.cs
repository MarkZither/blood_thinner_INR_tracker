using System;

namespace BloodThinnerTracker.Mobile.ViewModels
{
    public class InrListItemVm
    {
        public Guid PublicId { get; set; }
        public DateTime TestDate { get; set; }
        public decimal InrValue { get; set; }
        public string? Notes { get; set; }
        public bool ReviewedByProvider { get; set; }

        // Optional helper
        public string DisplayDate => TestDate.ToString("yyyy-MM-dd");
    }
}
