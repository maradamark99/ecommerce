using EcommerceLib.Contract;

namespace Shipping.Contract;

public class CreateShipmentRequest
{
    public string OrderId { get; set; }
    
    public string CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
    public CustomerDetails CustomerDetails { get; set; }
    public List<Item> Items { get; set; }
    public decimal Total { get; set; }
    public ShippingMethod ShippingMethod { get; set; }
}