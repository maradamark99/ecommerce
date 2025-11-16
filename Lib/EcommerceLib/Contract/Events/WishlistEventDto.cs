using EcommerceLib.Messaging;

namespace EcommerceLib.Contract.Events;

public class WishlistEventDto : IEventMessage
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string CorrelationId { get; set; }
    public string Id { get; set; }
    public List<string> CustomerIds { get; set; }
    public WishlistEventItemDto Item { get; set; }
    public DateTime Timestamp { get; set; }
}