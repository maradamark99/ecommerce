using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Data;

public class CustomerDetails
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string CustomerId { get; set; }
    
    public string Email { get; set; }
    
    public string FullName { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public Address? DefaultShippingAddress { get; set; }
    
    public Address? DefaultBillingAddress { get; set; }
}