using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Inventory.Contract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Inventory.Messaging;

public class OrderEventConsumerService(
    IOptions<OrderEventConsumerConfig> options,
    IMemoryCache cache,
    ILogger<OrderEventConsumerService> logger,
    IServiceScopeFactory scopeFactory) : EventConsumerBase<OrderEventDto>(options, cache, logger)
{
    protected override async Task HandleMessageAsync(OrderEventDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling Order event: {Id}, type {type}", dto.EventId, dto.EventType);
        var scope = scopeFactory.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        try
        {
            switch (dto.EventType)
            {
                case nameof(Events.OrderCancelled):
                    if (dto.Order.StatusHistory is [.., _, "Cancelled"] &&
                        (dto.Order.StatusHistory[^2] == "Created" ||
                        dto.Order.StatusHistory[^2] == "Pending"))
                    {
                        await inventoryService.ReleaseReservedStockAsync(dto.Order.OrderId);
                    }
                    break;
                case nameof(Events.OrderExpired):
                    await inventoryService.ReleaseReservedStockAsync(dto.Order.OrderId);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process order: {reason}", ex.Message);
        }
    }
}
