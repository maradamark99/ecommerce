using Ecommerce.Domain.Auth;
using Ecommerce.Domain.ProductManagement;

namespace Ecommerce.Domain.Review;

public class Review
{
    public long Id { get; set; }
    
    public decimal Rating { get; set; }
    
    public string Comment { get; set; }
    
    public AppUser Customer { get; set; }
    
    public Guid ProductId { get; set; }
    
    public Product Product { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; }
    
}