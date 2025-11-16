using Ecommerce.Common.Data;
using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.Shipping.Contract;

namespace Ecommerce.Domain.Shipping;

public class MockShippingService(IServiceScopeFactory serviceScopeFactory) : IShippingService
{
    public Task<ShippingRate> GetShippingRateAsync(ShippingMethod shippingMethod)
    {
        return Task.FromResult(new ShippingRate
        {
            Method = shippingMethod,
            Fee = 10.00m,
        });
    }

    public async Task CreateShipmentAsync(ShipmentRequest shipmentRequest)
    {
        await SimulateDeliveryAsync(shipmentRequest);
    }

    public Task CancelShipmentAsync(string shipmentId)
    {
        return Task.CompletedTask;
    }

    private async Task SimulateDeliveryAsync(ShipmentRequest shipmentRequest)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var order = await unitOfWork.Orders.GetByIdAsync(shipmentRequest.Order.Id);
        if (order != null)
        {
            order.StatusHistory.Add(new OrderStatus()
            {
                OrderId = order.Id,
                Status = Status.EnRoute
            });
            order.StatusHistory.Add(new OrderStatus()
            {
                OrderId = order.Id,
                Status = Status.Delivered
            });
            await unitOfWork.SaveChangesAsync();
        }
    }
}