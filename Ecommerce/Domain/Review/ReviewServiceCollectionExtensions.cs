using Ecommerce.Domain.Review.Contract;

namespace Ecommerce.Domain.Review;

public static class ReviewServiceCollectionExtensions
{
    public static IServiceCollection AddReviewServices(this IServiceCollection services)
    {
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        return services;
    }
}