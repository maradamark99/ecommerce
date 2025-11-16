using EcommerceLib.Contract.Dto;
using EcommerceLib.Messaging;

namespace EcommerceLib.Contract.Events;

public class CartCheckedOutEventDto : IEventMessage
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string CorrelationId { get; set; }
    public string CheckoutId { get; set; }
    public string CustomerId { get; set; }
    public List<CartItemDto> Items { get; set; }
    public CustomerDetailsDto CustomerDetails { get; set; }
    public AddressDto ShippingAddress { get; set; }
    public AddressDto BillingAddress { get; set; }
    public string PaymentMethod { get; set; }
    public string ShippingMethod { get; set; }
    public string? CustomerNotes { get; set; }
}