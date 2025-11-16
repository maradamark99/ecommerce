namespace Wishlist.Contract;

public class WishlistItemDto
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public decimal Price { get; set; }
    
    public bool IsAvailable { get; set; }   
    
    public DateTime AddedAt { get; set; }
}