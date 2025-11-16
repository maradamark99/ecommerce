namespace Inventory;

public class InventoryEntry
{
    public string Id { get; set; }
    
    public string ProductId { get; set; }
    
    public int AvailableQuantity { get; set; }
    
    public bool IsReorderNeeded { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}