using Ecommerce.Domain.Auth;

namespace Ecommerce.Domain.Wishlist;

public class Wishlist
{
    public Guid Id { get; set; }
    
    public AppUser Customer { get; set; }
    
    public IList<WishlistItem> Items { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}