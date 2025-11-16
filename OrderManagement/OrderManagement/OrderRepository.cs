using Microsoft.EntityFrameworkCore;
using OrderManagement.Common.Data;
using OrderManagement.Contract;

namespace OrderManagement;

public class OrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public async Task<string> CreateOrderAsync(Order order)
    {
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order.Id;
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
            .Include(o => o.CustomerDetails)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
    
    public async Task<Order?> GetByIdAsync(string customerId, string? checkoutId, string? orderId)
    {
        return await dbContext.Orders
            .Include(o => o.StatusHistory)
            .Include(o => o.CustomerDetails)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.CustomerDetails.CustomerId == customerId 
                                      && (orderId != null && o.Id == orderId || checkoutId != null && o.CheckoutId == checkoutId));
    }
    
    public async Task<IEnumerable<Order>> GetOrderHistoryAsync(string customerId)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .Include(o => o.CustomerDetails)
            .Where(o => o.CustomerDetails.CustomerId == customerId)
            .ToListAsync();
    }
    
}