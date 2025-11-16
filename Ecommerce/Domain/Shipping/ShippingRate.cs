namespace Ecommerce.Domain.Shipping;

public class ShippingRate
{
    public ShippingMethod Method { get; set; }
    public decimal Fee { get; set; }
    
    public DateTime? EstimatedShippingDate { get; set; }
}