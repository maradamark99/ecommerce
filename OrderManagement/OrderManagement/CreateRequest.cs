using EcommerceLib.Contract;
using Address = OrderManagement.Common.Data.Address;

namespace OrderManagement;

public class CreateOrderRequest
{
    public string CartId { get; set; }
    public CustomerDetailsRequest CustomerDetailsRequest { get; set; }
    public string PaymentMethod { get; set; }
    public ShippingMethod ShippingMethod { get; set; }
    
    public Address ShippingAddress { get; set; }
    
    public Address? BillingAddress { get; set; }
    
    public string? CustomerNotes { get; set; }
}