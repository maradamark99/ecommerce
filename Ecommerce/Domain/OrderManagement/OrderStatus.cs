namespace Ecommerce.Domain.OrderManagement;


public class OrderStatus
{
    public long Id { get; set; }
    
    public string OrderId { get; set; }
    
    public Status Status { get; set; }
    
}