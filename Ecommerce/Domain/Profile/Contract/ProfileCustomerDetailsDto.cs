using Ecommerce.Common.Data;

namespace Ecommerce.Domain.Profile.Contract;

public record ProfileCustomerDetailsDto(
    string FullName,
    string Email,
    string PhoneNumber,
    AddressDto DefaultShippingAddress,
    AddressDto? DefaultBillingAddress
);