using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderManagement.Contract;

namespace OrderManagement.Cart;

public class CartEventConsumerService(
    IOptions<CartEventConsumerConfig> options, 
    IMemoryCache cache, 
    ILogger<CartEventConsumerService> logger,
    IServiceScopeFactory serviceScopeFactory)
    : EventConsumerBase<CartCheckedOutEventDto>(options, cache, logger)
{
    protected override async Task HandleMessageAsync(CartCheckedOutEventDto msg, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling Cart event {Id}", msg.EventId);
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var orderManagementService = scope.ServiceProvider.GetRequiredService<IOrderManagementService>();
            switch (msg.EventType)
            {
                case nameof(Events.CartCheckedOut):
                    await orderManagementService.CreateOrderAsync(msg);
                    break;
                default:
                    throw new NotSupportedException($"{msg.EventType} is not supported");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process cart event: {reason}", ex.Message);
        }        
    }
}