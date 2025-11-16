using EcommerceLib.Messaging;

namespace EcommerceLib.Contract.Events;

public class InventoryEventDto : IEventMessage
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string CorrelationId { get; set; }
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime Timestamp { get; set; }
}