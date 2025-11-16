namespace Payment.Model;

public class Order
{
    public string OrderId { get; set; }
    
    public List<Product> Items { get; set; }

    public decimal TotalAmount { get; set; }
}