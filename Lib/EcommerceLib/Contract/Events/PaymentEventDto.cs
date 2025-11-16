using EcommerceLib.Messaging;

namespace EcommerceLib.Contract.Events;

public class PaymentEventDto : IEventMessage
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string CorrelationId { get; set; }
    public string OrderId { get; set; }
    public string PaymentId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Timestamp { get; set; }
    public string? FailureReason { get; set; }
}