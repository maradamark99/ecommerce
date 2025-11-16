namespace EcommerceLib.Contract.Dto;

public record CartItemDto
(
    string ProductId,
    int Quantity,
    decimal UnitPrice
);