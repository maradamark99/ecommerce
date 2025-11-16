namespace Inventory;

public class StockReservation
{
    public string OrderId { get; set; }
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime ExpiresAt { get; set; }
}