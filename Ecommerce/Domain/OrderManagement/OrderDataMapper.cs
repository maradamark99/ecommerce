using System.Buffers.Text;
using System.Text;
using Ecommerce.Common;
using Ecommerce.Common.Data;
using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.Payment;
using Ecommerce.Domain.Payment.Contract;
using Ecommerce.Domain.Shipping;
using Ecommerce.Domain.Shipping.Contract;

namespace Ecommerce.Domain.OrderManagement;

public class OrderDataMapper : IOrderDataMapper
{
    public CreateOrderRequest MapCreateRequestDtoToModel(
        CreateRequestDto createRequestDto)
    {
        if (createRequestDto == null)
        {
            throw new ArgumentNullException(nameof(createRequestDto), "CreateOrderDraftRequestDto cannot be null");
        }
        return new CreateOrderRequest()
        {
            CustomerDetailsRequest = MapCustomerDetailsDtoToModel(createRequestDto.CustomerDetails),
            ShippingAddress = CreateAddress(createRequestDto.ShippingAddress),
            BillingAddress = createRequestDto.BillingAddress == null ? null : CreateAddress(createRequestDto.BillingAddress),
            PaymentMethod = Enum.Parse<PaymentMethod>(createRequestDto.PaymentMethod, true),
            ShippingMethod = Enum.Parse<ShippingMethod>(createRequestDto.ShippingMethod, true),
            CustomerNotes = createRequestDto.CustomerNotes
        };
    }

    private static Address CreateAddress(AddressDto addressDto)
    {
        if (addressDto == null)
        {
            throw new ArgumentNullException(nameof(addressDto), "AddressDto cannot be null");
        }
        return new Address()
            {
                AddressLine = addressDto.AddressLine,
                City = addressDto.City,
                State = addressDto.State,
                PostalCode = addressDto.PostalCode,
                Country = addressDto.Country
            };
    }

    public OrderSummaryResponseDto MapOrderSummaryToDto(OrderSummary orderSummary)
    {
        if (orderSummary == null)
        {
            throw new ArgumentNullException(nameof(orderSummary), "OrderSummary cannot be null");
        }
        return new OrderSummaryResponseDto()
        {
            OrderId = orderSummary.OrderId,
            Items = orderSummary.Items,
            OrderStatus = orderSummary.Status.ToString(),
            CustomerDetails = MapCustomerDetailsToDto(orderSummary.CustomerDetails),
            PaymentRate = new PaymentRateDto(
                PaymentMethod: orderSummary.PaymentRate.PaymentMethod.ToString(),
                Fee: orderSummary.PaymentRate.Fee
            ),
            ShippingRate = new ShippingRateDto
            (
                ShippingMethod: orderSummary.ShippingRate.Method.ToString(),
                Fee: orderSummary.ShippingRate.Fee,
                EstimatedShippingDate: orderSummary.ShippingRate.EstimatedShippingDate?.ToString(Constants.DEFAULT_DATE_FORMAT) ?? string.Empty
            ),
            ShippingAddress = MapAddressToDto(orderSummary.ShippingAddress),
            BillingAddress = orderSummary.BillingAddress == null ? null : MapAddressToDto(orderSummary.BillingAddress)
        };
    }

    public OrderResponseDto MapOrderToResponseDto(Order? order)
    {
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order), "Order cannot be null");
        }

        return new OrderResponseDto(
            Id: order.Id,
            CustomerDetails: MapCustomerDetailsToDto(order.CustomerDetails),
            ShippingAddress: MapAddressToDto(order.ShippingAddress),
            BillingAddress: MapAddressToDto(order.BillingAddress),
            PaymentMethod: order.PaymentMethod,
            ShippingMethod: order.ShippingMethod,
            Total: order.Total,
            StatusHistory: order.StatusHistory.Select(s => s.Status.ToString()).ToList(),
            ShippingFee: order.ShippingFee,
            PaymentFee: order.PaymentFee,
            EstimatedDeliveryDate: order.EstimatedDeliveryDate?.ToString(),
            CustomerNotes: order.CustomerNotes
        );
    }

    private static AddressDto MapAddressToDto(Address address)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address), "Shipping address cannot be null");
        }

        return new AddressDto(
            AddressLine: address.AddressLine,
            City: address.City,
            State: address.State,
            PostalCode: address.PostalCode,
            Country: address.Country
        );
    }

    private static CustomerDetailsDto MapCustomerDetailsToDto(CustomerDetails customerDetails)
    {
        return new CustomerDetailsDto(
            FullName: customerDetails.FullName,
            Email: customerDetails.Email,
            PhoneNumber: customerDetails.PhoneNumber
        );
    }

    private static CustomerDetailsRequest MapCustomerDetailsDtoToModel(CustomerDetailsDto dto)
    {
        return new CustomerDetailsRequest()
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
        };
    }

}