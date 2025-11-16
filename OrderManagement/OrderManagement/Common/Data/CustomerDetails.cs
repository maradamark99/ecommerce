using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagement.Common.Data;

public class CustomerDetails
{
    public string CustomerId { get; set; }
    
    public string OrderId { get; set; }
    
    public string FullName { get; set; }
    
    public string Email { get; set; }
    
    public string PhoneNumber { get; set; }
}