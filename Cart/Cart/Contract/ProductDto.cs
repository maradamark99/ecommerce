namespace Cart.Contract;

public class ProductDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Condition { get; set; }
    public string PrimaryImageUrl { get; set; }
    public bool IsDiscounted { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public bool IsAvailable { get; set; }
}