using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;
using Wishlist.Contract;

namespace Wishlist;

public class WishlistService(
    IProductManagementClient productManagementClient,
    IWishlistRepository wishlistRepository,
    IEventProducer<WishlistEventDto> eventProducer) : IWishlistService
{
    
    public async Task<IEnumerable<WishlistItemDto>?> GetWishlistAsync(AppUser customer)
    {
        var wishlist = await wishlistRepository.GetWishlistForCustomerAsync(customer.Id);
        return wishlist?.Products.Select(item => new WishlistItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price,
            IsAvailable = item.IsAvailable,
            AddedAt = item.AddedAt
        });
    }

    public async Task AddToWishlistAsync(AppUser customer, string productId)
    {
        var productCatalogResponse = await productManagementClient.GetProductByIdAsync(productId);
        if (productCatalogResponse == null)
            throw new NotFoundException("Product not found");
        var wishlist = await wishlistRepository.GetWishlistForCustomerAsync(customer.Id);
        var product = new Product
        {
            Id = productCatalogResponse.Id,
            Name = productCatalogResponse.Name,
            Price = productCatalogResponse.Price,
            PrimaryImageUrl = productCatalogResponse.PrimaryImageUrl,
            IsListed = true,
            IsAvailable = productCatalogResponse.IsAvailable,
            AddedAt = DateTime.UtcNow
        };
        if (wishlist == null)
        {
            await wishlistRepository.CreateWishlistAsync(customer.Id, product);
        }
        else
        {
            await wishlistRepository.AddToWishlistAsync(wishlist, product);  
        }
    }

    public async Task RemoveFromWishlistAsync(AppUser customer, string productId)
    {
        var wishList = await wishlistRepository.GetWishlistForCustomerAsync(customer.Id);
        if (wishList == null)
            throw new NotFoundException("Wishlist not found");
        await wishlistRepository.RemoveFromWishlistAsync(wishList, productId);
    }

    public async Task UpdateProductAsync(ProductEventDto msg)
    {
        var product = await wishlistRepository.GetProductByIdAsync(msg.ProductId);
        if (product == null) 
            return;
        if (msg.Price < 0 || msg is { IsDiscounted: true, DiscountedPrice: < 0 })
            return;

        var wasAvailable = product.IsAvailable;
        var wasDiscounted = product.IsDiscounted;
        var previousDiscountedPrice = product.DiscountedPrice;

        product.Name = msg.ProductName;    
        product.Price = msg.Price;
        product.IsAvailable = msg.IsAvailable;
        product.IsDiscounted = msg.IsDiscounted;
        product.DiscountedPrice = msg.DiscountedPrice;
        
        if (msg.PrimaryImageUrl != null)
            product.PrimaryImageUrl = msg.PrimaryImageUrl;
        await wishlistRepository.UpdateProductAsync(product);

        if (!wasAvailable && msg.IsAvailable)
        {
            await NotifyWishlistersAsync(msg, nameof(Events.WishlistItemInStock));
        }
        else if (msg.IsAvailable && (!wasDiscounted && msg.IsDiscounted || msg.DiscountedPrice < previousDiscountedPrice))
        {
            await NotifyWishlistersAsync(msg, nameof(Events.WishlistItemDiscounted));
        }
    }
    
    public Task UpdateProductListingAsync(ProductEventDto msg, bool isListed)
    {
        return wishlistRepository.UpdateProductListingAsync(msg.ProductId, isListed);
    }

    private async Task NotifyWishlistersAsync(ProductEventDto msg, string eventType)
    {
        var product = await wishlistRepository.GetProductByIdAsync(msg.ProductId);
        if (product == null)
        {
            return;
        }
        var customersWhoHaveWishlistedTheProduct = await wishlistRepository.GetCustomersWhoHaveWishlistedTheProductAsync(msg.ProductId);
        if (customersWhoHaveWishlistedTheProduct.Count == 0)
        {
            return;
        }
        var dto = new WishlistEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            CorrelationId = msg.CorrelationId,
            CustomerIds = customersWhoHaveWishlistedTheProduct,
            Item = new WishlistEventItemDto()
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                IsAvailable = product.IsAvailable,
                IsDiscounted = product.IsDiscounted,
                DiscountedPrice = product.DiscountedPrice,
                PrimaryImageUrl = product.PrimaryImageUrl
            },
            Timestamp = DateTime.UtcNow,
        };
        await eventProducer.ProduceAsync(dto);
    }

}