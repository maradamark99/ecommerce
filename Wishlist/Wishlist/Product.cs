namespace Wishlist;

public class Product
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public decimal Price { get; set; }
    
    public decimal? DiscountedPrice { get; set; }
    
    public string PrimaryImageUrl { get; set; } 
    
    public bool IsListed { get; set; }
    
    public bool IsAvailable { get; set; }
    
    public bool IsDiscounted { get; set; }
    
    public DateTime AddedAt { get; set; }
}