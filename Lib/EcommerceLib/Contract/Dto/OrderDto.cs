namespace EcommerceLib.Contract.Dto;

public class OrderDto
{
    public string OrderId { get; set; }

    public CustomerDto Customer { get; set; }

    public List<ItemDto> Items { get; set; }

    public decimal TotalAmount { get; set; }

    public List<string> StatusHistory { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public string ShippingMethod { get; set; }
    
    public string PaymentMethod { get; set; }
}