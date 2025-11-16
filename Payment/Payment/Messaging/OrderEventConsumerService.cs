using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Payment.Contract;
using Payment.Model;

namespace Payment.Messaging;

public class OrderEventConsumerService(
    IOptions<ConsumerOptions> options, 
    IMemoryCache cache, 
    ILogger<OrderEventConsumerService> logger,
    IServiceScopeFactory serviceScopeFactory)
    : EventConsumerBase<OrderEventDto>(options, cache, logger)
{
    protected override async Task HandleMessageAsync(OrderEventDto msg, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received Order event: {Id}, type {type}", msg.EventId, msg.EventType);
        logger.LogInformation("Processing {EventType} event for OrderId: {OrderId}", msg.EventType, msg.Order.OrderId);
        var serviceScope = serviceScopeFactory.CreateScope();
        var paymentService = serviceScope.ServiceProvider.GetRequiredService<IPaymentService>();
        try
        {
            switch (msg.EventType)
            {
                case nameof(Events.OrderCreated):
                    await paymentService.CreateOrderAsync(new Order()
                    {
                        CustomerId = msg.Order.Customer.CustomerId,
                        OrderId = msg.Order.OrderId,
                        TotalAmount = msg.Order.TotalAmount,
                        SelectedPaymentMethod = Enum.Parse<PaymentMethod>(msg.Order.PaymentMethod)
                    });
                    break;
                case nameof(Events.OrderCompleted):
                    await paymentService.HandleOrderCompletedAsync(msg.Order);
                    break;
                case nameof(Events.OrderCancelled):
                    await paymentService.HandleOrderCancelledAsync(msg.Order);
                    break;
                default:
                    logger.LogWarning("Unknown event type: {EventType}", msg.EventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Order event");   
        }
    }
}