using EcommerceLib.Contract;
using EcommerceLib.Contract.Dto;

namespace Shipping.Contract;

public interface IShippingService
{
    public Task<ShippingRateDto> GetShippingFeeAsync(ShippingMethod shippingMethod);
    
    public Task CreateShipmentAsync(CreateShipmentRequest shipmentRequest);
    
    public Task CancelShipmentAsync(string orderId);
}