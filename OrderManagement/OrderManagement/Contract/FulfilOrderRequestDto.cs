namespace OrderManagement.Contract;

public record FulfilOrderRequestDto(
    string OrderId,
    string PaymentId
);