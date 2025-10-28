namespace BloodThinnerTracker.Shared.Models.Authentication
{
    public class GoogleOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = "/signin-google";
    }

    public class MicrosoftAccountOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = "/signin-microsoft";
    }
}
