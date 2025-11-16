namespace OrderManagement.Shipping.Contract;

public record ShippingFeeDto(
    string Method,
    decimal Fee,
    string? EstimatedShippingDate
);