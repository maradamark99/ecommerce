using Ecommerce.Common.Data;
using Ecommerce.Domain.Payment;
using Ecommerce.Domain.Shipping;

namespace Ecommerce.Domain.OrderManagement;

public class OrderSummary
{
    public string OrderId { get; set; }
    public IEnumerable<OrderItem> Items { get; set; }
    public Status Status { get; set; }
    
    public CustomerDetails CustomerDetails { get; set; }
    
    public Address ShippingAddress { get; set; }
    
    public Address? BillingAddress { get; set; }
    public PaymentRate PaymentRate { get; set; }
    
    public ShippingRate ShippingRate { get; set; }
    public decimal Total => Items.Select(i => i.UnitPrice * i.Quantity).Sum() + ShippingRate.Fee + PaymentRate.Fee;
    
}