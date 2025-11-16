using Microsoft.EntityFrameworkCore;

namespace Inventory;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<InventoryEntry> InventoryEntries { get; set; }
    public DbSet<StockReservation> StockReservations { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.HasKey(r => new { r.OrderId, r.ProductId });
        });
    }

}