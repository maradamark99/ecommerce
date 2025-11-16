using Ecommerce.Domain.Inventory.Contract;
using Ecommerce.Domain.OrderManagement;

namespace Ecommerce.Domain.Inventory;

public class NoOpInventoryService : IInventoryService
{
    public Task ReserveStockAsync(IEnumerable<OrderItem> orderItems)
    {
        return Task.CompletedTask;
    }

    public Task ReleaseStockAsync(IEnumerable<OrderItem> orderItems)
    {
        return Task.CompletedTask;
    }

    public Task UpdateStockAsync(IEnumerable<OrderItem> orderItems, Operation operation)
    {
        return Task.CompletedTask;
    }
}