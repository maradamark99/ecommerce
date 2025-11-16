using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Wishlist.Contract;

namespace Wishlist.Messaging;

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
        var wishlistService = scope.ServiceProvider.GetRequiredService<IWishlistService>();
        try
        {
            switch (msg.EventType)
            {
                case nameof(Events.ProductListingUpdated):
                    await wishlistService.UpdateProductAsync(msg);
                    break;
                case nameof(Events.ProductListed):
                    await wishlistService.UpdateProductListingAsync(msg, true);
                    break;
                case nameof(Events.ProductDelisted):
                    await wishlistService.UpdateProductListingAsync(msg, false);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process product event: {reason}", ex.Message);
        }
    }
}