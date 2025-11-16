using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Notification.Clients;
using Notification.Email;

namespace Notification.Consumers;

public class OrderEventConsumerService(
    IOptions<OrderEventConsumerConfig> options,
    IMemoryCache cache,
    ILogger<OrderEventConsumerService> logger,
    IEmailTemplateMapper emailTemplateMapper,
    IProfileClient profileClient,
    EmailNotificationService emailNotificationService) : EventConsumerBase<OrderEventDto>(options, cache, logger)
{
    protected override async Task HandleMessageAsync(OrderEventDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling notification {Id}", dto.EventId);
       
        try
        {
            if (dto.EventType is not 
                (nameof(Events.FulfillOrder) 
                or nameof(Events.OrderCancelled)
                or nameof(Events.OrderExpired)))
            {
                return;
            }
            var customerDetails = await profileClient.GetCustomerDetailsAsync(dto.Order.Customer.CustomerId);
            if (customerDetails == null)
            {
                return;
            }
            var payload = emailTemplateMapper.MapOrderEventToEmailPayload(dto, customerDetails);
            await emailNotificationService.NotifyAsync(payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to send notification: {reason}", ex.Message);
        }
    }
}