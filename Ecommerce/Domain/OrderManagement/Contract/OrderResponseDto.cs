using Ecommerce.Common.Data;

namespace Ecommerce.Domain.OrderManagement.Contract;

public record OrderResponseDto(
    string Id,
    CustomerDetailsDto CustomerDetails,
    AddressDto ShippingAddress,
    AddressDto? BillingAddress,
    string PaymentMethod,
    string ShippingMethod,
    decimal Total,
    List<string> StatusHistory,
    decimal? ShippingFee,
    decimal? PaymentFee,
    string? EstimatedDeliveryDate,
    string? CustomerNotes
);