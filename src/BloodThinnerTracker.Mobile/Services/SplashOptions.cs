using Microsoft.Extensions.Options;

namespace BloodThinnerTracker.Mobile.Services
{
    public class SplashOptions
    {
        public const string SectionName = "Splash";

        public bool ShowUntilInitialized { get; set; } = true;

        public int TimeoutMs { get; set; } = 3000;
    }

        public class SplashOptionsValidator : IValidateOptions<SplashOptions>
    {
        public ValidateOptionsResult Validate(string? name, SplashOptions options)
        {
            if (options == null)
                return ValidateOptionsResult.Fail("SplashOptions is null");

            if (options.TimeoutMs <= 0 || options.TimeoutMs > 60000)
            {
                return ValidateOptionsResult.Fail("Splash:TimeoutMs must be between 1 and 60000 milliseconds");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
