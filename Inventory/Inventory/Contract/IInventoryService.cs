namespace Inventory.Contract;

public interface IInventoryService
{
    Task CreateInventoryEntryForProductAsync(string productId, int initialStock = 0);
    
    Task<bool> IsProductInStockAsync(string productId, int quantity);
    
    Task<List<ReservationResult>> TryReserveStockAsync(StockReservationRequest stockReservationRequest);

    Task ReleaseReservedStockAsync(string orderId);

    Task UpdateStockAsync(string productId, int quantity, Operation operation);
}