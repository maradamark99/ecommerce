namespace OrderManagement.Payment;

public record PaymentFeeDto(
    string PaymentMethod,
    decimal Fee
);