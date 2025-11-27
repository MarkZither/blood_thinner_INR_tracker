using BloodThinnerTracker.Mobile.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BloodThinnerTracker.Mobile.Extensions
{
    public static class SplashOptionsExtensions
    {
        /// <summary>
        /// Register SplashOptions using the options pattern and attach validation.
        /// Keeps MauiProgram.cs concise by encapsulating registration logic here.
        /// </summary>
        public static IServiceCollection AddSplashOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<SplashOptions>()
                .Bind(configuration.GetSection(SplashOptions.SectionName));

            // Register explicit validator implementation
            services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<SplashOptions>, SplashOptionsValidator>();

            return services;
        }
    }
}
