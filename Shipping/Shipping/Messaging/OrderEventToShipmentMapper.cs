using EcommerceLib.Contract;
using EcommerceLib.Contract.Events;
using Shipping.Contract;

namespace Shipping.Messaging;

public class OrderEventToShipmentMapper : IOrderEventToShipmentMapper
{
    public CreateShipmentRequest OrderFulfilledToCreateShipmentRequestMapper(OrderEventDto dto)
    {
        var customer = dto.Order.Customer;
        return new CreateShipmentRequest()
        {
            OrderId = dto.Order.OrderId,
            Timestamp = dto.Timestamp,
            CustomerDetails = new CustomerDetails()
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                ShippingAddress = new Address()
                {
                    AddressLine = customer.ShippingAddress.AddressLine,
                    City = customer.ShippingAddress.City,
                    Country = customer.ShippingAddress.Country,
                    State = customer.ShippingAddress.State,
                    PostalCode = customer.ShippingAddress.PostalCode,
                }
            },
            Items = dto.Order.Items.Select(i => new Item()
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList(),
            Total = dto.Order.TotalAmount,
            ShippingMethod = Enum.Parse<ShippingMethod>(dto.Order.ShippingMethod, ignoreCase: true)
        };
    }
}