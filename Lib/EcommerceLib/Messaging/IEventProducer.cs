namespace EcommerceLib.Messaging;

public interface IEventProducer<in TMessage> where TMessage : IEventMessage
{
    Task ProduceAsync(TMessage message, CancellationToken cancellationToken = default);
}