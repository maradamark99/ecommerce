namespace Ecommerce.Domain.Wishlist.Contract;

public class WishlistItemDto
{
    public string ProductId { get; set; }
    
    public string ProductName { get; set; }
    
    public DateTime AddedAt { get; set; }
}