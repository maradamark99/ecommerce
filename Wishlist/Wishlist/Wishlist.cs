namespace Wishlist;

public class Wishlist
{
    public string Id { get; set; }
    
    public string CustomerId { get; set; }
    
    public IList<Product> Products { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}