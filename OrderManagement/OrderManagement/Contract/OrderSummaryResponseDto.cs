using EcommerceLib.Contract.Dto;
using OrderManagement.Payment;
using OrderManagement.Shipping.Contract;

namespace OrderManagement.Contract;

public class OrderSummaryResponseDto
{
    public string OrderId { get; set; }
    public IEnumerable<OrderItem> Items { get; set; }
    public string OrderStatus { get; set; }
    public CustomerDetailsDto CustomerDetails { get; set; }
    public PaymentFeeDto PaymentFee { get; set; }
    public ShippingFeeDto ShippingFee { get; set; }
    
    public AddressDto ShippingAddress { get; set; }
    
    public AddressDto? BillingAddress { get; set; }
    
    public decimal Total { get; set; }
}