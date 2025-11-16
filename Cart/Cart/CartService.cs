using Cart.Contract;
using EcommerceLib.Auth;
using EcommerceLib.Contract.Dto;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;

namespace Cart;

public class CartService(
    IEventProducer<CartCheckedOutEventDto> eventProducer,
    ICartMapper cartMapper,
    IProductManagementClient productManagementClient,
    ICartStore cartStore) : ICartService
{
    
    public async Task<CheckoutResponseDto> CheckoutAsync(AppUser user, CheckoutRequestDto dto)
    {
        var cart = await GetByIdAsync(user.Id);
        if (cart.Items.Count == 0)
            throw new BadRequestException("Your cart must contain at least one item.");
        if (cart.Items.Any(i => i.Quantity <= 0))
            throw new BadRequestException("All cart item quantities must be at least one.");
        if (cart.Items.Any(i => i.IsAvailable == false))
            throw new BadRequestException("Some items in your cart are no longer available.");
        var checkoutId = Guid.NewGuid().ToString();
        await eventProducer.ProduceAsync(CreateCheckedOutEvent(user, dto, checkoutId, cart));
        await ClearCartAsync(user.Id);
        return new CheckoutResponseDto
        (
            checkoutId, 
            cart, 
            dto.CustomerDetails, 
            dto.ShippingAddress, 
            dto.BillingAddress, 
            dto.ShippingMethod, 
            dto.PaymentMethod,
            dto.CustomerNotes
        );
    }

    public async Task<CartResponse> GetByIdAsync(string customerId)
    {
        var items = cartStore.GetCartForCustomer(customerId).ToList();
        if (items.Count == 0)
            return new CartResponse([]);

        var productIds = items.Select(i => i.ProductId).ToList();
        var products = new Dictionary<string, ProductDto>();

        var productsResponse = await productManagementClient.GetProductsByIdsAsync(productIds);
        foreach (var product in productsResponse)
        {
            products[product.Id] = product;
        }

        var newItems = items
            .Where(i => products.ContainsKey(i.ProductId))
            .ToList();
        cartStore.TryModifyCart(customerId, newItems);

        return new CartResponse(newItems
            .Select(i => cartMapper.ModelToResponse(i, products[i.ProductId]))
            .ToList()
        );
    }
    
    public Task ModifyCartAsync(string customerId, List<CartItem> items)
    {
        if (!items.TrueForAll(i => i.Quantity >= 0))
        {
            throw new BadRequestException("All quantities must be at least zero");
        }
        if (!cartStore.TryModifyCart(customerId, items))
        {
            throw new InternalServerErrorException("Cannot modify cart");
        }
        return Task.CompletedTask;
    }

    public Task ClearCartAsync(string customerId)
    {
        cartStore.TryClearCart(customerId);
        return Task.CompletedTask;
    }
    
    private static CartCheckedOutEventDto CreateCheckedOutEvent(AppUser user, CheckoutRequestDto dto, string checkoutId, CartResponse cart)
    {
        return new CartCheckedOutEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.CartCheckedOut),
            CheckoutId = checkoutId,
            CustomerId = user.Id,
            Items = cart.Items
                .Select(i => new CartItemDto(i.ProductId, i.Quantity, i.UnitPrice))
                .ToList(),
            CustomerDetails = dto.CustomerDetails,
            ShippingAddress = dto.ShippingAddress,
            BillingAddress = dto.BillingAddress!,
            PaymentMethod = dto.PaymentMethod,
            ShippingMethod = dto.ShippingMethod,
            CustomerNotes = dto.CustomerNotes,

        };
    }
    
}