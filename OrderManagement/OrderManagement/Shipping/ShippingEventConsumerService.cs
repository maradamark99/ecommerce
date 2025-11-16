using EcommerceLib;
using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderManagement.Contract;

namespace OrderManagement.Shipping;

public class ShippingEventConsumerService(
    IOptions<ShippingEventConsumerConfig> options,
    IMemoryCache cache,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ShippingEventConsumerService> logger)
    : EventConsumerBase<ShippingEventDto>(options, cache, logger)
{
    
    protected override async Task HandleMessageAsync(ShippingEventDto msg, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing Shipping event {Id}", msg.EventId);
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var orderManagementService = scope.ServiceProvider.GetRequiredService<IOrderManagementService>();
            switch (msg.EventType)
            {
                case nameof(Events.ShipmentDelivered):
                    await orderManagementService.UpdateStatusAsync(msg.OrderId, Status.Completed);
                    break;
                case nameof(Events.ShipmentFailed):
                    var user = new AppUser()
                    {
                        Id = msg.Customer.CustomerId,
                        Roles = [nameof(Roles.Customer)]
                    };
                    await orderManagementService.CancelOrderAsync(user, msg.OrderId, "Shipment failed.");
                    break;
                default:
                    throw new NotSupportedException($"{msg.EventType} is not supported");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process shipping event: {reason}", ex.Message);
        }    
    }
}