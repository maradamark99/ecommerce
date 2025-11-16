namespace Payment.Contract;

public class ProductDto
{
    public string ProductId { get; set; }
    public string Name { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}