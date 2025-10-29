public static class ServiceExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration config)
    {
        // Authentication services are configured directly in Program.cs for this feature.

        return services;
    }
}
