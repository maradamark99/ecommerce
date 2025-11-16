namespace Shipping.Contract;

public class CustomerDetails
{
    public string CustomerId { get; set; }
    
    public string FullName { get; set; }
    public Address ShippingAddress { get; set; }
}