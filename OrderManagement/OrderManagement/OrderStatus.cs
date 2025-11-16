using System.Text.Json.Serialization;

namespace OrderManagement;


public class OrderStatus
{
    public long Id { get; set; }
    
    public string OrderId { get; set; }
    
    public string Status { get; set; }
    
}