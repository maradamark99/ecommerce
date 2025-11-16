using Ecommerce.Common.Data;

namespace Ecommerce.Domain.OrderManagement.Contract;

public record CreateRequestDto(
    CustomerDetailsDto CustomerDetails,
    AddressDto ShippingAddress,
    AddressDto? BillingAddress,
    string ShippingMethod,
    string PaymentMethod,
    string? CustomerNotes
);