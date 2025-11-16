using System.Text;
using Ecommerce.Common.Exception;
using Ecommerce.Domain.Cart.Contract;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;

namespace Ecommerce.Domain.Cart;

public class CartService(
    ICartMapper cartMapper,
    IProductCatalogService productCatalogService,
    ICartStore cartStore) : ICartService
{
    public async Task<CartResponse> GetByIdAsync(string userId)
    {
        var cartItems = cartStore.GetCartForCustomer(userId);
        var cartItemsResponse = new List<CartItemResponse>();
        foreach (var item in cartItems)
        {
            var product = await productCatalogService.GetProductByIdAsync(item.ProductId);
            cartItemsResponse.Add(cartMapper.ModelToResponse(item, product));
        }

        return new CartResponse(userId, cartItemsResponse);
    }
    public Task ModifyCartAsync(string userId, List<CartItem> items)
    {
        if (!items.TrueForAll(i => i.Quantity >= 0))
        {
            throw new BadRequestException("All quantities must be at least zero");
        }
        if (!cartStore.TryModifyCart(userId, items))
        {
            throw new InternalServerErrorException("Cannot modify cart");
        }
        return Task.CompletedTask;
    }

    public Task ClearCartAsync(string userId)
    {
        cartStore.TryClearCart(userId);
        return Task.CompletedTask;
    }
    
}