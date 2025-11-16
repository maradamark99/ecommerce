using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Notification.Clients;
using Notification.Email;

namespace Notification.Consumers;

public class WishlistEventConsumerService(
    IOptions<WishlistEventConsumerConfig> options,
    IMemoryCache cache,
    ILogger<WishlistEventConsumerService> logger,
    IProfileClient profileClient,
    IEmailTemplateMapper emailTemplateMapper,
    EmailNotificationService emailNotificationService) 
    : EventConsumerBase<WishlistEventDto>(options, cache, logger){
    
    protected override async Task HandleMessageAsync(WishlistEventDto msg, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling notification {Id}", msg.EventId);
       
        try
        {
            if (msg.EventType is not (nameof(Events.WishlistItemDiscounted) or nameof(Events.WishlistItemInStock)))
            {
                return;
            }

            foreach (var customerId in msg.CustomerIds)
            {
                var customerDetails = await profileClient.GetCustomerDetailsAsync(customerId);
                if (customerDetails == null) 
                {
                    return;
                }
                var payload = emailTemplateMapper.MapWishlistEventToEmailPayload(msg, customerDetails);
                await emailNotificationService.NotifyAsync(payload);   
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to send notification: {reason}", ex.Message);
        }
    }
}