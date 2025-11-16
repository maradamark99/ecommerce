using EcommerceLib.Contract.Dto;

namespace Cart.Contract;

public record CheckoutResponseDto
(
    string CheckoutId,
    CartResponse CartContent,
    CustomerDetailsDto CustomerDetails,
    AddressDto ShippingAddress,
    AddressDto? BillingAddress,
    string ShippingMethod,
    string PaymentMethod,
    string? CustomerNotes
);