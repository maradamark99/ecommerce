using System.ComponentModel.DataAnnotations;

namespace OrderManagement;

public class CustomerDetailsRequest
{
    [Required]
    public string FullName { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [Phone]
    public string PhoneNumber { get; set; }
}