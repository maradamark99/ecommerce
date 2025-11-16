using Ecommerce.Common.Exception;
using Ecommerce.Common.File;
using Ecommerce.Common.Pagination;
using Ecommerce.Common.Storage;
using Ecommerce.Domain.ProductManagement.Category.Contract;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;
using Microsoft.Extensions.Options;

namespace Ecommerce.Domain.ProductManagement.ProductManagement;

public class ProductManagementService(
    IOptions<FileConfig> config,
    IOptions<ProductConfig> productConfig,
    IOptions<StorageConfig> storageConfig,
    IStorageClient storageClient,
    IFileValidator fileValidator,
    IFileInspector fileInspector,
    IProductManagementMapper productManagementMapper,
    IProductAttributesValidator attributeValidator,
    ICategoryRepository categoryRepository,
    IProductListingRepository productListingRepository,
    IProductManagementRepository productManagementRepository,
    IProductMediaRepository productMediaRepository
    ) : IProductManagementService, IProductMediaService, IProductListingService
{

    public async Task<Paged<ProductManagementResponse>> GetAllProductsAsync(Pager pager, Sorter sorter)
    {
        if (!Product.SortableColumns.Contains(sorter.SortBy.ToLower()))
        {
            throw new BadRequestException($"Invalid column name for sorting: {sorter.SortBy}");
        }
        var items = await productManagementRepository.GetAllAsync(pager, sorter);
        return items.Select(productManagementMapper.ModelToResponse);
    }

    public async Task<ProductManagementResponse> GetProductByIdAsync(string id)
    {
        var item = await productManagementRepository.GetByIdAsync(id);
        if (item is null)
        {
            throw new NotFoundException($"Item with id: {id} not found");
        }
        return productManagementMapper.ModelToResponse(item);
    }

    public async Task<string> CreateProductAsync(CreateProductRequestDto createProductRequestDto)
    {
        var category = await categoryRepository.GetByIdAsync(createProductRequestDto.CategoryId);
        if (category is null)
        {
            throw new NotFoundException($"Category with id: {createProductRequestDto.CategoryId} does not exist");
        }

        var item = productManagementMapper.RequestToModel(createProductRequestDto, category);
        var attributeDefinitions =
            await categoryRepository.GetCategoryAttributeDefinitionsAsync(category.Path);
        var validationResult = attributeValidator.Validate(item.Attributes, attributeDefinitions);

        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.ErrorMessage ?? "Product attributes validation failed");
        }

        var createdId = await productManagementRepository.CreateAsync(item);
        return createdId;
    }

    public async Task DeleteProductByIdAsync(string id)
    {
        if (!await productManagementRepository.ExistsByIdAsync(id))
        {
            throw new NotFoundException($"Product with id: {id} not found");
        }
        await productManagementRepository.DeleteByIdAsync(id);
    }
    
    public async Task UpdateProductAsync(string id, CreateProductRequestDto createProductRequestDto)
    {
        var existingProduct = await productManagementRepository.GetByIdAsync(id);
        if (existingProduct is null)
        {
            throw new NotFoundException($"Product with id: {id} not found");
        }

        var category = await categoryRepository.GetByIdAsync(createProductRequestDto.CategoryId);
        if (category is null)
        {
            throw new NotFoundException($"Category with id: {createProductRequestDto.CategoryId} does not exist");
        }

        var updatedProduct = productManagementMapper.RequestToModel(createProductRequestDto, category);
        updatedProduct.Id = existingProduct.Id;

        var attributeDefinitions = await categoryRepository.GetCategoryAttributeDefinitionsAsync(category.Path);
        var validationResult = attributeValidator.Validate(updatedProduct.Attributes, attributeDefinitions);
        
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.ErrorMessage ?? "Product attributes validation failed");
        }

        await productManagementRepository.UpdateAsync(id, updatedProduct);
    }

    public async Task<ProductMediaResponse> AddMediaAsync(string productId, IFormFile file, bool isPrimaryImage)
    {
        if (!await productManagementRepository.ExistsByIdAsync(productId))
        {
            throw new NotFoundException($"Product with id: {productId} not found");
        }
        var fileInfo = fileInspector.Inspect(file);
        var fileValidationResult = fileValidator.Validate(config.Value, fileInfo);
        if (!fileValidationResult.IsValid)
        {
            throw new BadRequestException(fileValidationResult.ErrorMessage ?? "File validation failed: file is not valid");
        }
        
        var key = $"{Guid.NewGuid()}{fileInfo.Extension}";
        var bucket = Buckets.ProductMedia;
        
        await storageClient.PutObjectAsync(bucket, key, file.OpenReadStream());
        var productMedia = new ProductMedia
        (
            file.FileName, 
            bucket, key, 
            Guid.Parse(productId), 
            fileInfo.Type, 
            isPrimaryImage
        );
        await productMediaRepository.AddMediaAsync(productId, productMedia);
        return new ProductMediaResponse
        (
            productMedia.Id,
            $"{storageConfig.Value.Endpoint}/{productMedia.Bucket}/{productMedia.Key}",
            productMedia.FileType,
            IsFileAnImage(file.ContentType) && productMedia.IsPrimaryImage
        );
    }

    public async Task RemoveMediaAsync(string productId, long mediaId)
    {
        if (!await productManagementRepository.ExistsByIdAsync(productId))
        {
            throw new NotFoundException($"Product with id: {productId} not found");
        }
        var media = await productMediaRepository.GetProductMediaByIdAsync(productId, mediaId);
        if (media is null)
        {
            throw new NotFoundException($"Media with id: {mediaId} for product with id: {productId} not found");
        }
        await storageClient.RemoveObjectAsync(media.Bucket, media.Key);
        await productMediaRepository.RemoveMediaAsync(productId, mediaId);
    }

    public async Task<IEnumerable<ProductListingResponse>?> GetProductListingHistoryAsync(string productId)
    {
        var productListingHistory = await productListingRepository.GetProductListingHistoryAsync(productId);
        return productListingHistory?.Select(p => new ProductListingResponse(
            p.Id,
            p.PriceInEur,
            p.IsActive,
            p.CreatedAt,
            p.UpdatedAt,
            p.Product.Id.ToString(),
            p.Product.Name,
            p.Product.Description
        ));
    }

    public async Task ListProductAsync(ProductListingRequest productListingRequest)
    {
        var productId = productListingRequest.ProductId;
        var product = await productManagementRepository.GetByIdAsync(productId);
        if (product is null)
        {
            throw new NotFoundException($"Product with id: {productId} not found");
        }
        if (product.Media?.Count < productConfig.Value.MinimumMediaCount)
        {
            throw new BadRequestException("Product does not enough have images.");
        }
        if (await productListingRepository.IsProductAlreadyListedAsync(productId))
        {
            throw new ConflictException($"Product with id: {productId} already listed");
        }
        await productListingRepository.ListProductAsync(productId, new ProductListing(productListingRequest.PriceInEur));
    }
    
    public async Task DelistProductAsync(string productId)
    {
        if (!await productManagementRepository.ExistsByIdAsync(productId) ||
            !await productListingRepository.IsProductAlreadyListedAsync(productId)) {
            throw new NotFoundException($"Product with id: {productId} not found or not listed");
        }
        await productListingRepository.DelistProductAsync(productId);
    }
    
    private bool IsFileAnImage(string contentType)
    {
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
    
}