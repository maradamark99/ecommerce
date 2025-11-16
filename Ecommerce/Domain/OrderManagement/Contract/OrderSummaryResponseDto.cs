using Ecommerce.Common.Data;
using Ecommerce.Domain.Payment.Contract;
using Ecommerce.Domain.Shipping.Contract;

namespace Ecommerce.Domain.OrderManagement.Contract;

public class OrderSummaryResponseDto
{
    public string OrderId { get; set; }
    public IEnumerable<OrderItem> Items { get; set; }
    public string OrderStatus { get; set; }
    public CustomerDetailsDto CustomerDetails { get; set; }
    public PaymentRateDto PaymentRate { get; set; }
    public ShippingRateDto ShippingRate { get; set; }
    
    public AddressDto ShippingAddress { get; set; }
    
    public AddressDto? BillingAddress { get; set; }
    
    public decimal Total { get; set; }
}