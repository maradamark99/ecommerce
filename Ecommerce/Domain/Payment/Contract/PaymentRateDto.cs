namespace Ecommerce.Domain.Payment.Contract;

public record PaymentRateDto(
    string PaymentMethod,
    decimal Fee
);