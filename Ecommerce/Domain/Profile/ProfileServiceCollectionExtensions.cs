using Ecommerce.Domain.Profile.Contract;

namespace Ecommerce.Domain.Profile;

public static class ProfileServiceCollectionExtensions
{
    public static IServiceCollection AddProfileServices(this IServiceCollection services)
    {
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        return services;
    }
}