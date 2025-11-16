namespace Ecommerce.Domain.Shipping;

public class TrackingInformation
{
    public string TrackingId { get; set; }
    
    public string Carrier { get; set; }
    
    public DateTime EstimatedDeliveryDate { get; set; }
    
    public string Status { get; set; }
}