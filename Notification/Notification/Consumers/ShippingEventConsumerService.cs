using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Notification.Clients;
using Notification.Email;

namespace Notification.Consumers;

public class ShippingEventConsumerService(
    IOptions<ShippingEventConsumerConfig> options,
    IMemoryCache cache,
    ILogger<ShippingEventConsumerService> logger,
    IProfileClient profileClient,
    IEmailTemplateMapper emailTemplateMapper,
    EmailNotificationService emailNotificationService) 
    : EventConsumerBase<ShippingEventDto>(options, cache, logger)
{
    
    protected override async Task HandleMessageAsync(ShippingEventDto msg, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling notification {Id}", msg.EventId);
       
        try
        {
            if (msg.EventType is not 
                (nameof(Events.ShipmentCreated) 
                or nameof(Events.ShipmentDelivered)))
            {
                return;
            }
            var customerDetails = await profileClient.GetCustomerDetailsAsync(msg.Customer.CustomerId);
            if (customerDetails == null)
            {
                return;
            }
            var payload = emailTemplateMapper.MapShippingEventToEmailPayload(msg, customerDetails!);
            await emailNotificationService.NotifyAsync(payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to send notification: {reason}", ex.Message);
        }    
    }
}