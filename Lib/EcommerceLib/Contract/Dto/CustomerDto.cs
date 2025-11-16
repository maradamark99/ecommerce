namespace EcommerceLib.Contract.Dto;

public class CustomerDto
{
    public string CustomerId { get; set; }

    public string FullName { get; set; }
    
    public string Email { get; set; }
    
    public string PhoneNumber { get; set; }
    
    public AddressDto ShippingAddress { get; set; }
    
    public AddressDto BillingAddress { get; set; }
}