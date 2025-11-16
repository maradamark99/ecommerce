using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Common.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{

    public DbSet<CustomerDetails> CustomerDetails { get; set; }

    public DbSet<OrderItem> OrderItems { get; set; }
    
    public DbSet<Order> Orders { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<CustomerDetails>()
            .HasKey(x => new { x.CustomerId, x.OrderId });
        
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