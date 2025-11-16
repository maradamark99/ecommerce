using EcommerceLib.Contract;
using EcommerceLib.Contract.Dto;
using OrderManagement.Common;
using OrderManagement.Common.Data;
using OrderManagement.Contract;
using OrderManagement.Payment;
using OrderManagement.Shipping.Contract;
using CustomerDetailsDto = OrderManagement.Contract.CustomerDetailsDto;

namespace OrderManagement;

using CustomerDetailsDto = CustomerDetailsDto;

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
            CartId = createRequestDto.CartId,
            CustomerDetailsRequest = MapCustomerDetailsDtoToModel(createRequestDto.CustomerDetails),
            ShippingAddress = CreateAddress(createRequestDto.ShippingAddress),
            BillingAddress = createRequestDto.BillingAddress == null ? null : CreateAddress(createRequestDto.BillingAddress),
            PaymentMethod = createRequestDto.PaymentMethod,
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
            PaymentFee = new PaymentFeeDto(
                PaymentMethod: orderSummary.PaymentFee.Method,
                Fee: orderSummary.PaymentFee.Fee
            ),
            ShippingFee = new ShippingFeeDto
            (
                Method: orderSummary.ShippingRate.Method,
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
            CartId: order.CheckoutId ?? string.Empty,
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

        return new AddressDto()
        {
            AddressLine = address.AddressLine,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country
        };
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