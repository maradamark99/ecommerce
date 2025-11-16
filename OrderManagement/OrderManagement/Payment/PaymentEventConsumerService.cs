using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderManagement.Contract;

namespace OrderManagement.Payment;

public class PaymentEventConsumerService(
    IOptions<PaymentEventConsumerConfig> options, 
    IMemoryCache cache, 
    ILogger<PaymentEventConsumerService> logger,
    IServiceScopeFactory serviceScopeFactory)
    : EventConsumerBase<PaymentEventDto>(options, cache, logger)
{
    protected override async Task HandleMessageAsync(PaymentEventDto msg, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling Payment event {Id}", msg.EventId);
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var orderManagementService = scope.ServiceProvider.GetRequiredService<IOrderManagementService>();
            switch (msg.EventType)
            {
                case nameof(Events.PaymentSucceeded):
                case nameof(Events.PaymentPendingOnDelivery):
                    await orderManagementService.FulfillOrderAsync(msg.OrderId);
                    break;
                case nameof(Events.PaymentFailed):
                    var user = new AppUser()
                    {
                        Id = msg.CustomerId,
                        Roles = [nameof(Roles.Customer)]
                    };
                    await orderManagementService.CancelOrderAsync(user, msg.OrderId, "Payment failed.");
                    break;
                default:
                    throw new NotSupportedException($"{msg.EventType} is not supported");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process payment event: {reason}", ex.Message);
        }        
    }
}