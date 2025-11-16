namespace EcommerceLib.Contract.Dto;

public class ItemDto
{
    public string ProductId { get; set; }

    public string? Name { get; set; }

    public int Quantity { get; set; }

    public decimal? Price { get; set; }
    
    public string Condition { get; set; }
}