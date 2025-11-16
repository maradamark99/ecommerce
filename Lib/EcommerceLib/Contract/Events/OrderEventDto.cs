using EcommerceLib.Contract.Dto;
using EcommerceLib.Messaging;

namespace EcommerceLib.Contract.Events;

public class OrderEventDto : IEventMessage
{
    public string EventId { get; set; }

    public string EventType { get; set; }

    public string CorrelationId { get; set; }

    public DateTime Timestamp { get; set; }

    public OrderDto Order { get; set; }
    
    public string? FailureReason { get; set; }
}