using Ecommerce.Domain.OrderManagement;

namespace Ecommerce.Domain.Inventory.Contract;

public interface IInventoryService
{
    Task ReserveStockAsync(IEnumerable<OrderItem> orderItems);
    Task ReleaseStockAsync(IEnumerable<OrderItem> orderItems);
}