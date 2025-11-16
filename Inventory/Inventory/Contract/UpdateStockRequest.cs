namespace Inventory.Contract;

public record UpdateStockRequest
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public string Operation { get; set; }
}