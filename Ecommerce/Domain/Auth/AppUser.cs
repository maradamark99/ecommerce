using Ecommerce.Common.Data;
using Ecommerce.Domain.OrderManagement;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Domain.Auth;

public class AppUser : IdentityUser
{
    
    public List<RefreshToken>? RefreshTokens { get; set; }
    
    public List<Review.Review>? Reviews { get; set; }
    
    public List<Order>? OrderHistory { get; set; }
    
    public CustomerDetails? CustomerDetails { get; set; }
    
}