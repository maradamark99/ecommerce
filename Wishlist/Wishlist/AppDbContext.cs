using Microsoft.EntityFrameworkCore;

namespace Wishlist;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    
    public DbSet<Product> Products { get; set; }
    
    public DbSet<Wishlist> Wishlists { get; set; }  
    
}