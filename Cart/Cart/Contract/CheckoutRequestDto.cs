using EcommerceLib.Contract.Dto;

namespace Cart.Contract;

public record CheckoutRequestDto(
    CustomerDetailsDto CustomerDetails,
    AddressDto ShippingAddress,
    AddressDto? BillingAddress,
    string ShippingMethod,
    string PaymentMethod,
    string? CustomerNotes
);