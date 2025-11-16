using Microsoft.EntityFrameworkCore;
using Payment.Contract;

namespace Payment;

public class PaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public Task<Model.Payment?> GetPaymentByIdAsync(string id)
    {
        return dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task CreatePaymentAsync(Model.Payment payment)
    {
        await dbContext.Payments.AddAsync(payment);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdatePaymentAsync(Model.Payment payment)
    {
        dbContext.Payments.Update(payment);
        await dbContext.SaveChangesAsync();
    }

    public async Task CreateOrderAsync(Order order)
    {
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();    
    }

    public async Task<Order?> GetOrderAsync(string customerId, string orderId)
    {
        return await dbContext.Orders
            .Include(co => co.Payment)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.OrderId == orderId);
    }

    public Task RemoveOrderAsync(string customerId, string orderId)
    {
        return dbContext.Orders
            .Where(c => c.CustomerId == customerId && c.OrderId == orderId)
            .ExecuteDeleteAsync();
    }
}