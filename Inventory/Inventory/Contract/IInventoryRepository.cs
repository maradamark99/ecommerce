namespace Inventory.Contract;

public interface IInventoryRepository
{
    Task CreateInventoryEntryAsync(InventoryEntry entry);  
    
    Task<InventoryEntry?> GetInventoryEntryByProductIdAsync(string productId);
    
    Task<List<StockReservation>> GetStockReservationByOrderIdAsync(string orderId);
    
    Task<bool> TryReserveStockAsync(StockReservation reservation);

    Task<bool> TryReleaseReservedStockAsync(string orderId, string productId);
    
    Task UpdateStockAsync(string productId, int quantity);
}