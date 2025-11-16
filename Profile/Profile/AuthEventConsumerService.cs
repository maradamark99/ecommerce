using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Profile.Contract;

namespace Profile;

public class AuthEventConsumerService(
    IOptions<ConsumerOptions> options, 
    IMemoryCache cache, 
    IServiceScopeFactory serviceScopeFactory,
    ILogger<AuthEventConsumerService> logger)
    : EventConsumerBase<AuthEventDto>(options, cache, logger)
{
    protected override async Task HandleMessageAsync(AuthEventDto msg, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received Auth event: {EventType} for UserId: {UserId}", msg.EventType, msg.CustomerId);
        using var scope = serviceScopeFactory.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        try {
            await profileService.HandleEmailConfirmedAsync(msg.CustomerId, msg.Email);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unable to process auth event: {reason}", ex.Message);
        }
    }
}