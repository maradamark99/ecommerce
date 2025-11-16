namespace Ecommerce.Domain.Payment.Contract;

public record PaymentRequestDto(string OrderId, string PaymentMethod);