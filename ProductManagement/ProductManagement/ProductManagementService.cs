using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.File;
using EcommerceLib.Messaging;
using EcommerceLib.Pagination;
using EcommerceLib.Storage;
using Microsoft.Extensions.Options;
using ProductManagement.Category.Contract;
using ProductManagement.Contract;
using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;

namespace ProductManagement;

public class ProductManagementService(
    IOptions<FileConfig> config,
    IOptions<ProductConfig> productConfig,
    IOptions<StorageConfig> storageConfig,
    IStorageClient storageClient,
    IFileValidator fileValidator,
    IFileInspector fileInspector,
    IProductManagementMapper productManagementMapper,
    IProductAttributesValidator attributeValidator,
    IProductManagementRepository productManagementRepository,
    IProductMediaRepository productMediaRepository,
    IProductListingRepository productListingRepository,
    ICategoryRepository categoryRepository,
    IEventProducer<ProductEventDto> productEventProducer
    ) : IProductManagementService, IProductMediaService, IProductListingService
{

    public async Task<Paged<ProductManagementResponse>> GetAllProductsAsync(Pager pager, Sorter sorter)
    {
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
        if (!Enum.TryParse(createProductRequestDto.ProductCondition, true, out Condition condition))
        {
            throw new BadRequestException($"Invalid product condition: {createProductRequestDto.ProductCondition}");
        }
        var category = await categoryRepository.GetByIdAsync(createProductRequestDto.CategoryId);
        if (category is null)
        {
            throw new NotFoundException($"Category with id: {createProductRequestDto.CategoryId} does not exist");
        }
        
        var product = productManagementMapper.RequestToModel(createProductRequestDto, category, 
            await CreateCombinedAttributesAsync(createProductRequestDto));
        var attributeDefinitions =
            await categoryRepository.GetCategoryAttributeDefinitionsAsync(category.Path);
        var validationResult = attributeValidator.Validate(product.Attributes, attributeDefinitions);

        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.ErrorMessage ?? "Invalid and/or missing product attributes.");
        }

        var createdId = await productManagementRepository.CreateAsync(product);
        await productEventProducer.ProduceAsync(new ProductEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ProductCreated),
            ProductId = createdId,
            ProductName = product.Name
        });
        return createdId;
    }

    public async Task DeleteProductByIdAsync(string id)
    {
        var product = await productManagementRepository.GetByIdAsync(id);
        if (await productListingRepository.IsProductAlreadyListedAsync(id))
        {
            throw new BadRequestException("You cannot delete a product that is listed.");
        }
        if (product is null)
        {
            throw new NotFoundException($"Product with id: {id} not found");
        }
        await productManagementRepository.DeleteByIdAsync(id);
        await productEventProducer.ProduceAsync(new ProductEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ProductDeleted),
            ProductId = product.Id,
            ProductName = product.Name
        });
    }
    
    public async Task UpdateProductAsync(string id, CreateProductRequestDto createProductRequestDto)
    {
        var existingProduct = await productManagementRepository.GetByIdAsync(id);
        if (existingProduct is null)
        {
            throw new NotFoundException($"Product with id: {id} not found");
        }
        if (await productListingRepository.IsProductAlreadyListedAsync(id))
        {
            throw new BadRequestException("You cannot update a product that is listed.");
        }

        var category = await categoryRepository.GetByIdAsync(createProductRequestDto.CategoryId);
        if (category is null)
        {
            throw new NotFoundException($"Category with id: {createProductRequestDto.CategoryId} does not exist");
        }

        var updatedProduct = productManagementMapper.RequestToModel(createProductRequestDto, category, 
            await CreateCombinedAttributesAsync(createProductRequestDto));
        updatedProduct.Id = existingProduct.Id;

        var attributeDefinitions = await categoryRepository.GetCategoryAttributeDefinitionsAsync(category.Path);
        var validationResult = attributeValidator.Validate(updatedProduct.Attributes, attributeDefinitions);
        
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.ErrorMessage ?? "Invalid and/or missing attributes.");
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
        var productMedia = new ProductMedia(file.FileName, bucket, key, productId, fileInfo.Type, isPrimaryImage);
        await productMediaRepository.AddMediaAsync(productId, productMedia);
        return new ProductMediaResponse(
            productMedia.Id,
            $"{storageConfig.Value.Endpoint}/{productMedia.Bucket}/{productMedia.Key}",
            productMedia.FileType,
            productMedia.IsPrimaryImage
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

        if (await productListingRepository.IsProductAlreadyListedAsync(productId) && media.IsPrimaryImage)
        {
            throw new BadRequestException("You cannot remove the primary image of an already listed product");
        }
        await storageClient.RemoveObjectAsync(media.Bucket, media.Key);
        await productMediaRepository.RemoveMediaAsync(productId, mediaId);
    }

    public async Task<IEnumerable<ProductListingResponse>?> GetProductListingHistoryAsync(string productId)
    {
        var productListingHistory = await productListingRepository.GetProductListingHistoryAsync(productId);
        return productListingHistory?.Select(p => new ProductListingResponse(
            p.Id,
            p.Price,
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
        if (!product.IsInStock) 
        {
            throw new BadRequestException("Cannot list a product that not available.");
        }
        await productListingRepository.ListProductAsync(productId, new ProductListing(productListingRequest.PriceInEur));
        var primaryImage = product.Media?.FirstOrDefault(m => m.IsPrimaryImage);
        var primaryImageUrl = primaryImage != null
            ? $"{storageConfig.Value.Endpoint}/{primaryImage.Bucket}/{primaryImage.Key}"
            : "";

        await productEventProducer.ProduceAsync(new ProductEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ProductListed),
            ProductId = product.Id,
            ProductName = product.Name,
            PrimaryImageUrl = primaryImageUrl,
            Price = productListingRequest.PriceInEur
        });
    }
    
    public async Task DelistProductAsync(string productId)
    {
        if (!await productListingRepository.IsProductAlreadyListedAsync(productId)) {
            throw new NotFoundException($"Product with id: {productId} not listed");
        }
        await productListingRepository.DelistProductAsync(productId);
        await productEventProducer.ProduceAsync(new ProductEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ProductDelisted),
            ProductId = productId,
        });
    }

    public Task<bool> IsProductListedAsync(string productId)
    {
        return productListingRepository.IsProductAlreadyListedAsync(productId);
    }

    public async Task ApplyDiscountAsync(string productId, DiscountRequest discountRequest)
    {
        var listing = await productListingRepository.GetActiveProductListingAsync(productId); 
        if (listing is null)
        {
            throw new NotFoundException($"Product with id: {productId} not listed");
        }
        if (discountRequest.Percentage is <= 0 or >= 100)
        {
            throw new BadRequestException("Discount percentage must be between 0 and 100");
        }
        var discountedPriceInEur = listing.Price - (listing.Price * discountRequest.Percentage / 100);
        listing.DiscountedPrice = discountedPriceInEur;
        listing.IsDiscounted = true;
        await productListingRepository.UpdateProductListingAsync(listing);
        await productEventProducer.ProduceAsync(new ProductEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ProductListingUpdated),
            ProductId = productId,
            ProductName = listing.Product.Name,
            IsAvailable = listing.Product.IsInStock,
            Price = listing.Price,
            DiscountedPrice = discountedPriceInEur,
            IsDiscounted = discountedPriceInEur < listing.Price,
        });
    }
    
    public async Task UpdateStockAvailabilityAsync(string productId, bool isInStock)
    {
        var product = await productManagementRepository.GetByIdAsync(productId);
        if (product == null)
        {
            throw new NotFoundException($"Product with id: {productId} not found");
        }
        if (product.IsInStock == isInStock)
        {
            return;
        }
        product.IsInStock = isInStock;
        await productManagementRepository.UpdateAsync(productId, product);
        await productEventProducer.ProduceAsync(new ProductEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ProductListingUpdated),
            ProductId = product.Id,
            ProductName = product.Name,
            IsAvailable = isInStock
        });
    }
    
    private async Task<List<Attribute>> CreateCombinedAttributesAsync(CreateProductRequestDto createProductRequestDto)
    {
        var existingAttributes = await productManagementRepository.GetExistingAttributesAsync(createProductRequestDto.Attributes.Select(a => new Attribute()
        {
            Name = a.Name,
            Value = a.Value
        }).ToList()) ?? new List<Attribute>();
        
        var newAttributes = createProductRequestDto.Attributes
            .Where(a => !existingAttributes.Any(ea => ea.Name == a.Name && ea.Value == a.Value))
            .Select(a => new Attribute()
            {
                Name = a.Name,
                Value = a.Value
            }).ToList();
        
        var combinedAttributes = existingAttributes.Concat(newAttributes).ToList();
        return combinedAttributes;
    }
}
