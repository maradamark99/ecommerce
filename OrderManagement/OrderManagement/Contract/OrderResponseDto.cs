using EcommerceLib.Contract.Dto;

namespace OrderManagement.Contract;

public record OrderResponseDto(
    string Id,
    string CartId,
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