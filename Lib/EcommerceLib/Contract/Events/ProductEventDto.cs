using EcommerceLib.Messaging;

namespace EcommerceLib.Contract.Events;

public class ProductEventDto : IEventMessage
{
    public string EventId { get; set; }
    public string EventType { get; set;  }
    public string CorrelationId { get; set; }
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsDiscounted { get; set; }
    public decimal? DiscountedPrice { get; set; }
}