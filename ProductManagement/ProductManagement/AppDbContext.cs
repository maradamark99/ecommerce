using Microsoft.EntityFrameworkCore;
using ProductManagement.Category;
using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;

namespace ProductManagement;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AttributeDefinition> AttributeDefinitions { get; set; }
    public DbSet<Category.Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    
    public DbSet<ProductListing> ProductListings { get; set; }
    
    public DbSet<Attribute> ProductAttributes { get; set; }
    
    public DbSet<ProductMedia> ProductMedia { get; set; }
    
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