using Microsoft.EntityFrameworkCore;

namespace Payment;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Model.Payment> Payments { get; set; }
    
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasKey(c => new { c.CustomerId, c.OrderId });
    }
}