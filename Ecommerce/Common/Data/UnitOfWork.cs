using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.ProductManagement.Category.Contract;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;
using Ecommerce.Domain.Profile.Contract;
using Ecommerce.Domain.Review.Contract;
using Ecommerce.Domain.Wishlist.Contract;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ecommerce.Common.Data
{
    public class UnitOfWork(
        AppDbContext context,
        ICategoryRepository categories,
        IOrderRepository orders,
        IProductCatalogRepository productCatalog,
        IProductListingRepository productListings,
        IProductManagementRepository productManagement,
        IProductMediaRepository productMedia,
        IProfileRepository profile,
        IReviewRepository reviews,
        IWishlistRepository wishlist
    ): IUnitOfWork
    {
        public ICategoryRepository Categories { get; } = categories;
        public IOrderRepository Orders { get; } = orders;
        public IProductCatalogRepository ProductCatalog { get; } = productCatalog;
        public IProductListingRepository ProductListings { get; } = productListings;
        public IProductManagementRepository ProductManagement { get; } = productManagement;
        public IProductMediaRepository ProductMedia { get; } = productMedia;
        public IProfileRepository Profile { get; } = profile;
        public IReviewRepository Reviews { get; } = reviews;
        public IWishlistRepository Wishlist { get; } = wishlist;    

        private IDbContextTransaction? _transaction;

        public async Task<bool> SaveChangesAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync(); 
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            context.Dispose();
        }
    }
}