using Ecommerce.Common.Data;
using Ecommerce.Domain.Auth;

namespace Ecommerce.Domain.OrderManagement;

public class Order
{
    public string Id { get; set; }
    
    public AppUser AppUser { get; set; }
    public CustomerDetails CustomerDetails { get; set; }
    
    public List<OrderItem> OrderItems { get; set; }
    
    public List<OrderStatus> StatusHistory { get; set; }
    
    public Address ShippingAddress { get; set; }
    
    public Address BillingAddress { get; set; }
    
    public string PaymentMethod { get; set; }
    
    public string ShippingMethod { get; set; }
    
    public string? PaymentIntentId { get; set; }
    
    public decimal? ShippingFee { get; set; }
    
    public decimal? PaymentFee { get; set; }
    
    public string? CustomerNotes { get; set; }
    
    public decimal ItemTotal => OrderItems.Sum(item => item.UnitPrice * item.Quantity);
    
    public decimal Total => ItemTotal + (ShippingFee ?? 0) + (PaymentFee ?? 0);
    public Status CurrentStatus => StatusHistory.LastOrDefault()?.Status ?? Status.Pending;
    public DateTime? EstimatedDeliveryDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }

}