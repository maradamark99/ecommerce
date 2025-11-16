using System.ComponentModel;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.ProductManagement;
using Ecommerce.Domain.ProductManagement.Category;
using Ecommerce.Domain.Review;
using Ecommerce.Domain.Wishlist;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Common.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{

    public DbSet<AppRole> AppRoles { get; set; }
    
    public DbSet<Address> Addresses { get; set; }
    
    public DbSet<Category> Categories { get; set; }
    
    public DbSet<AttributeDefinition> AttributeDefinitions { get; set; }
    
    public DbSet<CustomerDetails> CustomerDetails { get; set; }

    public DbSet<OrderItem> OrderItems { get; set; }
    
    public DbSet<Order> Orders { get; set; }
    
    public DbSet<Product> Products { get; set; }
    
    public DbSet<ProductListing> ProductListings { get; set; }
    
    public DbSet<ProductAttribute> ProductAttributes { get; set; }
    
    public DbSet<ProductMedia> ProductMedia { get; set; }
    
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    public DbSet<Review> Reviews { get; set; }
    
    public DbSet<Wishlist> Wishlists { get; set; }
    
    public DbSet<WishlistItem> WishlistItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
        {
            if (foreignKey.IsRequired) 
                foreignKey.DeleteBehavior = DeleteBehavior.Cascade;
            else
                foreignKey.DeleteBehavior = DeleteBehavior.SetNull;
        }
    }
    
}