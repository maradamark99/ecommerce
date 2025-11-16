using EcommerceLib.Contract.Dto;

namespace OrderManagement.Contract;

public record CreateRequestDto(
    string CartId,
    CustomerDetailsDto CustomerDetails,
    AddressDto ShippingAddress,
    AddressDto? BillingAddress,
    string ShippingMethod,
    string PaymentMethod,
    string? CustomerNotes
);