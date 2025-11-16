using Inventory.Contract;

namespace Inventory;

public class InventoryService(IInventoryRepository inventoryRepository) : IInventoryService
{
    public Task CreateInventoryEntryForProductAsync(string productId, int initialStock = 0)
    {
        var entry = new InventoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = productId,
            AvailableQuantity = initialStock,
            IsReorderNeeded = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return inventoryRepository.CreateInventoryEntryAsync(entry);
    }

    public async Task<bool> IsProductInStockAsync(string productId, int quantity)
    {
        var entry = await inventoryRepository.GetInventoryEntryByProductIdAsync(productId);
        return entry != null && entry.AvailableQuantity >= quantity;
    }

    public async Task<List<ReservationResult>> TryReserveStockAsync(StockReservationRequest stockReservationRequest)
    {
        var results = new List<ReservationResult>();
        foreach (var item in stockReservationRequest.Items)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(Constants.RESERVATION_EXPIRY_MINUTES);
            var isSuccess = await inventoryRepository.TryReserveStockAsync(new StockReservation() 
            {
                OrderId = stockReservationRequest.OrderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                ExpiresAt = expiresAt 
            }); 
            results.Add(new ReservationResult
            {
                OrderId = stockReservationRequest.OrderId,
                ProductId = item.ProductId,
                IsSuccess = isSuccess,
                ExpiresAt = expiresAt,
                ErrorMessage = isSuccess ? null : "Insufficient stock or product not found" 
            });
        }
        if (!results.All(r => r.IsSuccess))
        {
            results.ForEach(async r =>
            {
                if (r.IsSuccess)
                {
                    await ReleaseReservedStockAsync(stockReservationRequest.OrderId);
                    r.ErrorMessage = "Reservation for failed, so all reservations have been rolled back.";
                }
            });
        }
        return results;
    }

    public async Task ReleaseReservedStockAsync(string orderId)
    {
        var reservations = inventoryRepository.GetStockReservationByOrderIdAsync(orderId);
        foreach (var reservation in reservations.Result.Where(reservation => reservation.ExpiresAt > DateTime.UtcNow))
        {
            await inventoryRepository.TryReleaseReservedStockAsync(orderId, reservation.ProductId);
        }
    }

    public Task UpdateStockAsync(string productId, int quantity, Operation operation)
    {
        return operation switch
        {
            Operation.Add => inventoryRepository.UpdateStockAsync(productId, quantity),
            Operation.Subtract => inventoryRepository.UpdateStockAsync(productId, -quantity),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }
    
}