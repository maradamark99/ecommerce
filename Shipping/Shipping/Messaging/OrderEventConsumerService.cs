using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shipping.Contract;

namespace Shipping.Messaging;

public class OrderEventConsumerService(
    IOptions<ConsumerOptions> options,
    IMemoryCache cache,
    IOrderEventToShipmentMapper shipmentMapper,
    IShippingService shippingService,
    ILogger<OrderEventConsumerService> logger) : EventConsumerBase<OrderEventDto>(options, cache, logger)
{
    protected override async Task HandleMessageAsync(OrderEventDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling shipment for Order event: {Id}", dto.EventId);
        try
        {
            switch (dto.EventType)
            {
                case nameof(Events.FulfillOrder):
                    await shippingService.CreateShipmentAsync
                    (
                        shipmentMapper.OrderFulfilledToCreateShipmentRequestMapper(dto)
                    );
                    break;
                case nameof(Events.OrderCancelled):
                    await shippingService.CancelShipmentAsync(dto.Order.OrderId);
                    break;
                default:
                    throw new NotSupportedException($"{dto.EventType} is not supported");    
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process order event: {reason}", ex.Message);
        }
    }
}