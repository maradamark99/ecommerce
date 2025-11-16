using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Review.Contract;

namespace Review.Messaging;

public class ProductEventConsumerService(
    IOptions<ProductEventConsumerConfig> options,
    IMemoryCache cache, 
    ILogger<ProductEventConsumerService> logger, 
    IServiceScopeFactory scopeFactory)
    : EventConsumerBase<ProductEventDto>(options, cache, logger)
{
    protected override async Task HandleMessageAsync(ProductEventDto msg, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling Product event: {Id}, type {type}", msg.EventId, msg.EventType);
        var scope = scopeFactory.CreateScope();
        var reviewService = scope.ServiceProvider.GetRequiredService<IReviewService>();
        try
        {
            switch (msg.EventType)
            {
                case nameof(Events.ProductListed):
                    await reviewService.UpdateProductListingAsync(msg, true);
                    break;
                case nameof(Events.ProductDelisted):
                    await reviewService.UpdateProductListingAsync(msg, false);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process product event: {reason}", ex.Message);
        }
    }
}