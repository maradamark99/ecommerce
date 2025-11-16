namespace Cart.Contract;

public record CartResponse(List<CartItemResponse> Items)
{
    public decimal TotalPrice =>
        Items.Sum(i => i.UnitPrice * i.Quantity);
}