using EcommerceLib.Contract.Dto;
using OrderManagement.Common.Data;
using OrderManagement.Payment;

namespace OrderManagement;

public class OrderSummary
{
    public string OrderId { get; set; }
    public IEnumerable<OrderItem> Items { get; set; }
    public Status Status { get; set; }
    
    public CustomerDetails CustomerDetails { get; set; }
    
    public Address ShippingAddress { get; set; }
    
    public Address? BillingAddress { get; set; }
    public PaymentFee PaymentFee { get; set; }
    
    public ShippingRateDto ShippingRate { get; set; }
    public decimal Total => Items.Select(i => i.UnitPrice * i.Quantity).Sum() + ShippingRate.Fee + PaymentFee.Fee;
    
}