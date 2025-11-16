namespace ProductManagement.Contract;

public class DiscountRequest
{
    public decimal Percentage { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
}