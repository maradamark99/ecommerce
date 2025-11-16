namespace Ecommerce.Common.Messaging;

public interface IEventHandler<TEvent, TPayload> where TEvent : IEvent<TPayload>
{
    Task HandleAsync(TEvent @event);
}