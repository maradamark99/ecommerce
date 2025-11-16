using Ecommerce.Common.Data;
using Ecommerce.Domain.OrderManagement.Contract;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Domain.OrderManagement;

public class OrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public async Task<string> CreateOrderAsync(Order order)
    {
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order.Id;
    }
    
    public async Task FulfillOrderAsync(Order order)
    {
        await dbContext.SaveChangesAsync();
    }

    public Task UpdateOrderAsync(Order order)
    {
        dbContext.Orders.Update(order);
        return dbContext.SaveChangesAsync();
    }

    public async Task<Order?> GetByIdAsync(string orderId)
    {
        return await dbContext.Orders
            .Include(o => o.StatusHistory)
            .Include(o => o.AppUser)
            .Include(o => o.CustomerDetails)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<IEnumerable<Order>> GetOrderHistoryAsync(string customerId)
    {
        return await dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .Where(o => o.AppUser != null && o.AppUser.Id == customerId)
            .ToListAsync();
    }
}