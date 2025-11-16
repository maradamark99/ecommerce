using EcommerceLib.Contract.Events;

namespace Shipping.Contract;

public interface IOrderEventToShipmentMapper
{
    CreateShipmentRequest OrderFulfilledToCreateShipmentRequestMapper(OrderEventDto dto);
}