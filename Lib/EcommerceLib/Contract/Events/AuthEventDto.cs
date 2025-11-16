using EcommerceLib.Messaging;

namespace EcommerceLib.Contract.Events;

public class AuthEventDto : IEventMessage
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string CorrelationId { get; set; }
    public string CustomerId { get; set; }
    public string Email { get; set; }
    public DateTime Timestamp { get; set; }
}