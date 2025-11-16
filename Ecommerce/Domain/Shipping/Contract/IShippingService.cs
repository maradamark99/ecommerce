namespace Ecommerce.Domain.Shipping.Contract;

public interface IShippingService
{
    public Task<ShippingRate> GetShippingRateAsync(ShippingMethod shippingMethod);
    
    public Task CreateShipmentAsync(ShipmentRequest shipmentRequest);
    
    public Task CancelShipmentAsync(string orderId);
}