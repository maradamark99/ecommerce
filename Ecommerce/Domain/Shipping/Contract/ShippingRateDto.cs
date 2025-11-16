namespace Ecommerce.Domain.Shipping.Contract;

public record ShippingRateDto(
    string ShippingMethod,
    decimal Fee,
    string? EstimatedShippingDate
);