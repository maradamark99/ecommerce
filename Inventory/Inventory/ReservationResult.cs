namespace Inventory;

public class ReservationResult
{
    public string OrderId { get; set; }
    public string ProductId { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExpiresAt { get; set; }
}