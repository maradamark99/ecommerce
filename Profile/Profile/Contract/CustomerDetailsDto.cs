using EcommerceLib.Contract.Dto;

namespace Profile.Contract;

public record CustomerDetailsDto(
    string FullName,
    string PhoneNumber,
    AddressDto DefaultShippingAddress,
    AddressDto? DefaultBillingAddress);