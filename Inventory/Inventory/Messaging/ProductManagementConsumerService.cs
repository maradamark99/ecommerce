using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Inventory.Contract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Inventory.Messaging;

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
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        try
        {
            if (msg.EventType != nameof(Events.ProductCreated)) 
            {
                logger.LogInformation("Ignoring unsupported product event type: {type}", msg.EventType);
                return;
            }
            await inventoryService.CreateInventoryEntryForProductAsync(msg.ProductId, initialStock: 0);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process product event: {reason}", ex.Message);
        }
    }
}