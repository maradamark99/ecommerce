using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProductManagement.Contract;

namespace ProductManagement.Messaging;

public class InventoryEventConsumerService(
    IOptions<ConsumerOptions> options, 
    IMemoryCache cache, 
    ILogger<InventoryEventConsumerService> logger,
    IServiceScopeFactory scopeFactory)
    : EventConsumerBase<InventoryEventDto>(options, cache, logger)
{
    protected override async Task HandleMessageAsync(InventoryEventDto msg, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received Inventory Event: {EventType} for ProductId: {ProductId} with Quantity: {Quantity}",
            msg.EventType, msg.ProductId, msg.Quantity);
        var scope = scopeFactory.CreateScope();
        var productManagementService = scope.ServiceProvider.GetRequiredService<IProductListingService>();
        try
        {
            switch (msg.EventType)
            {
                case nameof(Events.InventoryInStock):
                case nameof(Events.InventoryOutOfStock):
                    await productManagementService.UpdateStockAvailabilityAsync(msg.ProductId, msg.Quantity > 0);  
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process inventory event: {reason}", ex.Message);
        }
    }
}