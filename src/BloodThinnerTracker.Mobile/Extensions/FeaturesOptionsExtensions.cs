using BloodThinnerTracker.Mobile.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BloodThinnerTracker.Mobile.Extensions
{
    public static class FeaturesOptionsExtensions
    {
        /// <summary>
        /// Register FeaturesOptions using the options pattern.
        /// Binds Features:* configuration section from appsettings.json or environment variables.
        /// </summary>
        public static IServiceCollection AddFeaturesOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<FeaturesOptions>()
                .Bind(configuration.GetSection(FeaturesOptions.SectionName));

            return services;
        }
    }
}
