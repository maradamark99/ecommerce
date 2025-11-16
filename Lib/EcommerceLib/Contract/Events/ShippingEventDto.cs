using EcommerceLib.Contract.Dto;
using EcommerceLib.Messaging;

namespace EcommerceLib.Contract.Events;

public class ShippingEventDto : IEventMessage
{
    
    public string EventId { get; set; }
    
    public string EventType { get; set; }
    
    public string CorrelationId { get; set; }
    
    public string ShipmentId { get; set; }
    
    public string OrderId { get; set; }
    
    public string ShipmentStatus { get; set; }
    
    public CustomerDto Customer { get; set; }
    
    public List<ItemDto> Items { get; set; }
    
    public string? FailureReason { get; set; }
}