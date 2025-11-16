using OrderManagement.Shipping.Contract;

namespace OrderManagement.Shipping;

public class ShippingFeeResult
{
    public System.Net.HttpStatusCode StatusCode { get; set; }
    public ShippingFeeDto? Fee { get; set; }
}