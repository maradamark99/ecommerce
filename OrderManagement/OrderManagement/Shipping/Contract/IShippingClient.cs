namespace OrderManagement.Shipping.Contract;

public interface IShippingClient
{
    Task<ShippingFeeResult> GetShippingFeeAsync(string shippingMethod);
}