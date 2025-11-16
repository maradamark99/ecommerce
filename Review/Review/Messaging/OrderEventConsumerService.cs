using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Review.Contract;

namespace Review.Messaging;

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
        var reviewService = scope.ServiceProvider.GetRequiredService<IReviewService>();
        try
        {
            switch (dto.EventType)
            {
               case nameof(Events.OrderCompleted):
                   await reviewService.SaveCustomerPurchasesAsync(dto);
                   break;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process order event: {reason}", ex.Message);
        }
    }
}
