using Microsoft.EntityFrameworkCore;
using Review.Model;

namespace Review;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Model.Review> Reviews { get; set; }
    
    public DbSet<Customer> Customers { get; set; }
    
    public DbSet<CustomerPurchase> CustomerPurchases { get; set; }
    
    public DbSet<Product> Products { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Customer>()
            .HasKey(c => c.Id);
        
        modelBuilder.Entity<CustomerPurchase>()
            .HasKey(cp => new { cp.CustomerId, cp.ProductId });
        
        modelBuilder.Entity<Model.Review>()
            .HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Model.Review>()
            .HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        
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