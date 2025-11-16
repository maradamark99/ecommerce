namespace Ecommerce.Common.Messaging;

public interface IEventBus
{
    public Task RegisterHandlerAsync<TPayload>(EventHandler<IEvent<TPayload>> handler) where TPayload : notnull;
    
    public Task PublishAsync<TPayload>(IEvent<TPayload> @event) where TPayload : notnull;
}