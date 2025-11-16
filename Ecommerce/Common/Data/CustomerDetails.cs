using Ecommerce.Domain.Auth;

namespace Ecommerce.Common.Data;

public class CustomerDetails
{
    public long Id { get; set; }
    
    public string FullName { get; set; }
    
    public string Email { get; set; }
    
    public string PhoneNumber { get; set; }
    
    public string AppUserId { get; set; }
    
    public Address? DefaultShippingAddress { get; set; }
    
    public Address? DefaultBillingAddress { get; set; }
}