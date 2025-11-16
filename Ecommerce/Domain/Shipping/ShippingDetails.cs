using Ecommerce.Common.Data;

namespace Ecommerce.Domain.Shipping;

public class ShippingDetails
{
    public Address ShippingAddress { get; set; }
    public ShippingMethod ShippingMethod { get; set; }
}