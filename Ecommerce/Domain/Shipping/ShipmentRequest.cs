using Ecommerce.Common.Data;
using Ecommerce.Domain.OrderManagement;

namespace Ecommerce.Domain.Shipping;

public class ShipmentRequest
{
    public Order Order { get; set; }
}