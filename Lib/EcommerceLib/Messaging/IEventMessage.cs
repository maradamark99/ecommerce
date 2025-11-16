namespace EcommerceLib.Messaging;

public interface IEventMessage
{
    public string EventId { get; }
    
    public string EventType { get; }
    
    public string CorrelationId { get; }
}