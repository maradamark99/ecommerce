using Inventory.Contract;
using Microsoft.EntityFrameworkCore;

namespace Inventory;

public class InventoryRepository(AppDbContext dbContext) : IInventoryRepository
{
    public Task CreateInventoryEntryAsync(InventoryEntry entry)
    {
        dbContext.InventoryEntries.Add(entry);    
        return dbContext.SaveChangesAsync();
    }

    public Task<InventoryEntry?> GetInventoryEntryByProductIdAsync(string productId)
    {
        return dbContext.InventoryEntries.FirstOrDefaultAsync(e => e.ProductId == productId);
    }

    public Task<List<StockReservation>> GetStockReservationByOrderIdAsync(string orderId)
    {
        return dbContext.StockReservations.Where(r => r.OrderId == orderId).ToListAsync();
    }

    public async Task<bool> TryReserveStockAsync(StockReservation reservation)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try 
        {
            var entry = await dbContext.InventoryEntries
                .FirstOrDefaultAsync(p => p.ProductId == reservation.ProductId && p.AvailableQuantity >= reservation.Quantity);
            if (entry == null)
            {
                await transaction.RollbackAsync();
                return false;
            }
            entry.AvailableQuantity -= reservation.Quantity;
            await dbContext.StockReservations.AddAsync(reservation);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
        return true;
    }

    public async Task<bool> TryReleaseReservedStockAsync(string orderId, string productId)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var reservation = await dbContext.StockReservations
                .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ProductId == productId);

            if (reservation == null)
            {
                await transaction.RollbackAsync();
                return true;
            }
            
            var entry = await dbContext.InventoryEntries
                .FirstOrDefaultAsync(p => p.ProductId == reservation.ProductId);
            if (entry == null)
            {
                await transaction.RollbackAsync();
                return false;
            }
            entry.AvailableQuantity += reservation.Quantity;

            dbContext.StockReservations.Remove(reservation);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task UpdateStockAsync(string productId, int quantity)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var entry = await dbContext.InventoryEntries
            .SingleAsync(p => p.ProductId == productId);
        
        if (entry.AvailableQuantity + quantity < 0)
        {
            entry.AvailableQuantity = 0;
        }
        else
        {
            entry.AvailableQuantity += quantity;
        }

        await dbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }

}