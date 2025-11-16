namespace Ecommerce.Domain.OrderManagement.Contract;

public record FulfilOrderRequestDto(
    string OrderId,
    string PaymentId
);