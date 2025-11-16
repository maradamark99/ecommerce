using Ecommerce.Common.Data;
using Ecommerce.Domain.Payment;
using Ecommerce.Domain.Shipping;

namespace Ecommerce.Domain.OrderManagement;

public class CreateOrderRequest
{
    public CustomerDetailsRequest CustomerDetailsRequest { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public ShippingMethod ShippingMethod { get; set; }
    
    public Address ShippingAddress { get; set; }
    
    public Address? BillingAddress { get; set; }
    
    public string? CustomerNotes { get; set; }
}