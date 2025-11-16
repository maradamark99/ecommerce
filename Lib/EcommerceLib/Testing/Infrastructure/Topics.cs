namespace EcommerceLib.Testing.Infrastructure;

// TODO: this should come from a config file
public static class Topics
{
    public static readonly string OrderEvents = "order-events";
    public static readonly string ProductEvents = "product-events";
    public static readonly string WishlistEvents = "wishlist-events";
    public static readonly string ShippingEvents = "shipping-events";
    public static readonly string PaymentEvents = "payment-events";
    public static readonly string InventoryEvents = "inventory-events";
    public static readonly string CartEvents = "cart-events";

    public static List<string> All()
    {
        return [OrderEvents, ProductEvents, WishlistEvents, ShippingEvents, PaymentEvents, InventoryEvents, CartEvents];
    }
}