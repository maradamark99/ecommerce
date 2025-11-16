using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.ProductManagement.Category.Contract;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;
using Ecommerce.Domain.Profile.Contract;
using Ecommerce.Domain.Review.Contract;
using Ecommerce.Domain.Wishlist.Contract;

namespace Ecommerce.Common.Data;

public interface IUnitOfWork
{
    ICategoryRepository Categories { get; }
    IOrderRepository Orders { get; }
    IProductCatalogRepository ProductCatalog { get; }
    IProductListingRepository ProductListings { get; }
    IProductManagementRepository ProductManagement { get; }
    IProductMediaRepository ProductMedia { get; }
    IProfileRepository Profile { get; }
    IReviewRepository Reviews { get; }
    IWishlistRepository Wishlist { get; }
    Task<bool> SaveChangesAsync();

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}