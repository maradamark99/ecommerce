using Ecommerce.Domain.ProductManagement;

namespace Ecommerce.Domain.Wishlist;

public class WishlistItem
{
    public long Id { get; set; }
    
    public Guid ProductId { get; set; }
    
    public Product Product { get; set; }
    
    public DateTime AddedAt { get; set; }
}