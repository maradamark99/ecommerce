namespace EcommerceLib.Contract.Events;

public class WishlistEventItemDto
{
    public string ProductId { get; set; }
    public string Name { get; set; }
    public string PrimaryImageUrl { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }   
    public bool IsDiscounted { get; set; }
    public decimal? DiscountedPrice { get; set; }
}