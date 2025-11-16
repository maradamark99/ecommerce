namespace Ecommerce.Common.Data;

public record AddressDto(
    string Country,
    string State,
    string City,
    string PostalCode,
    string AddressLine
);