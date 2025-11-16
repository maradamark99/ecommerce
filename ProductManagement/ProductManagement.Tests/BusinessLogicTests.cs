using EcommerceLib;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.File;
using EcommerceLib.Messaging;
using EcommerceLib.Pagination;
using EcommerceLib.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using ProductManagement.Category;
using ProductManagement.Category.Contract;
using ProductManagement.Contract;
using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;
using FileInfo = EcommerceLib.File.FileInfo;

namespace ProductManagement.Tests;

public class BusinessLogicTests
{
    private Mock<IOptions<FileConfig>> _fileConfigMock;
    private Mock<IOptions<ProductConfig>> _productConfigMock;
    private Mock<IOptions<StorageConfig>> _storageConfigMock;
    private Mock<IStorageClient> _storageClientMock;
    private Mock<IFileValidator> _fileValidatorMock;
    private Mock<IFileInspector> _fileInspectorMock;
    private Mock<IProductManagementMapper> _productManagementMapperMock;
    private Mock<IProductAttributesValidator> _productAttributesValidatorMock;
    private Mock<IProductManagementRepository> _productManagementRepositoryMock;
    private Mock<IProductMediaRepository> _productMediaRepositoryMock;
    private Mock<IProductListingRepository> _productListingRepositoryMock;
    private Mock<ICategoryRepository> _categoryRepositoryMock;
    private Mock<IEventProducer<ProductEventDto>> _productEventProducerMock;

    private ProductManagementService _service;

    [SetUp]
    public void Setup()
    {
        _fileConfigMock = new Mock<IOptions<FileConfig>>();
        _productConfigMock = new Mock<IOptions<ProductConfig>>();
        _storageConfigMock = new Mock<IOptions<StorageConfig>>();
        _storageClientMock = new Mock<IStorageClient>();
        _fileValidatorMock = new Mock<IFileValidator>();
        _fileInspectorMock = new Mock<IFileInspector>();
        _productManagementMapperMock = new Mock<IProductManagementMapper>();
        _productAttributesValidatorMock = new Mock<IProductAttributesValidator>();
        _productManagementRepositoryMock = new Mock<IProductManagementRepository>();
        _productMediaRepositoryMock = new Mock<IProductMediaRepository>();
        _productListingRepositoryMock = new Mock<IProductListingRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _productEventProducerMock = new Mock<IEventProducer<ProductEventDto>>();
        
        _fileConfigMock.Setup(c => c.Value).Returns(new FileConfig());
        _storageConfigMock.Setup(s => s.Value).Returns(new StorageConfig { Endpoint = "https://cdn.example.com" });
        _productConfigMock.Setup(p => p.Value).Returns(new ProductConfig { MinimumMediaCount = 1 });

        _service = new ProductManagementService(
            _fileConfigMock.Object,
            _productConfigMock.Object,
            _storageConfigMock.Object,
            _storageClientMock.Object,
            _fileValidatorMock.Object,
            _fileInspectorMock.Object,
            _productManagementMapperMock.Object,
            _productAttributesValidatorMock.Object,
            _productManagementRepositoryMock.Object,
            _productMediaRepositoryMock.Object,
            _productListingRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productEventProducerMock.Object
        );
    }

    [Test]
    public async Task GetAllProductsAsync_ShouldCallRepository_WhenSortByIsValid()
    {
        // Arrange
        var pager = new Pager { PageNumber = 1, PageSize = 10};
        var sorter = new Sorter { SortBy = "name" };
        var fakeProduct = new Product
        {
            Id = "P123",
            Name = "Test Product",
            Description = "A product for testing",
            Condition = Condition.New,
        }; 
        var products = new List<Product> { new() };
        var pagedProducts = new Paged<Product>()
        {
            Items = products,
            Page = pager.PageNumber,
            PageSize = pager.PageSize,
            TotalItems = products.Count
        };

        _productManagementRepositoryMock
            .Setup(r => r.GetAllAsync(pager, sorter))
            .ReturnsAsync(pagedProducts);

        _productManagementMapperMock
            .Setup(m => m.ModelToResponse(It.IsAny<Product>()))
            .Returns(new ProductManagementResponse(
                Id: fakeProduct.Id,
                Name: fakeProduct.Name,
                Description: fakeProduct.Description,
                Condition: fakeProduct.Condition.ToString(),
                CategoryId: 2,
                Attributes: new List<ProductAttributeResponse>
                {
                    new("Color", "Red"),
                    new("Size", "L")
                },
                PrimaryImageUrl: "https://cdn.example.com/images/p123.jpg",
                MediaUrls: ["https://cdn.example.com/images/p123-1.jpg"]
            ));

        // Act
        var result = await _service.GetAllProductsAsync(pager, sorter);
        
        // Assert
        _productManagementRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<Pager>(), It.IsAny<Sorter>()), Times.Once);
        _productManagementMapperMock.Verify(m => m.ModelToResponse(It.IsAny<Product>()), Times.Exactly(products.Count));
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TotalItems, Is.EqualTo(products.Count));
    }
    
    [Test]
    public async Task GetProductByIdAsync_ShouldReturnMappedResponse_WhenProductExists()
    {
        // Arrange
        const string productId = "P123";
        var fakeProduct = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "A product for testing",
            Condition = Condition.New,
        };

        var expectedResponse = new ProductManagementResponse(
            Id: fakeProduct.Id,
            Name: fakeProduct.Name,
            Description: fakeProduct.Description,
            Condition: fakeProduct.Condition.ToString(),
            CategoryId: 2,
            Attributes: new List<ProductAttributeResponse>(),
            PrimaryImageUrl: null,
            MediaUrls: null
        );

        _productManagementRepositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(fakeProduct);

        _productManagementMapperMock
            .Setup(m => m.ModelToResponse(fakeProduct))
            .Returns(expectedResponse);

        // Act
        var result = await _service.GetProductByIdAsync(productId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(fakeProduct.Id));
        Assert.That(result.Name, Is.EqualTo(fakeProduct.Name));

        _productManagementRepositoryMock.Verify(r => r.GetByIdAsync(productId), Times.Once);
        _productManagementMapperMock.Verify(m => m.ModelToResponse(fakeProduct), Times.Once);
    }

    [Test]
    public void GetProductByIdAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        const string productId = "NonExistentId";
        _productManagementRepositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(default(Product));

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.GetProductByIdAsync(productId));

        Assert.That(ex.Message, Is.EqualTo($"Item with id: {productId} not found"));
        _productManagementRepositoryMock.Verify(r => r.GetByIdAsync(productId), Times.Once);
        _productManagementMapperMock.Verify(m => m.ModelToResponse(It.IsAny<Product>()), Times.Never);
    }
    
    [Test]
    public async Task CreateProductAsync_ShouldReturnCreatedId_WhenAllValid()
    {
        // Arrange
        var requestDto = new CreateProductRequestDto("prod1", "sample", 2, new List<ProductAttributeRequest>(), nameof(Condition.New));

        var category = new Category.Category() { Id = 42, Path = "category/path" };
        var productModel = new Product { Name = requestDto.Name, Attributes = new List<Attribute>() };
        var createdId = "P123";

        _categoryRepositoryMock.Setup(c => c.GetByIdAsync(requestDto.CategoryId))
            .ReturnsAsync(category);
        _categoryRepositoryMock.Setup(c => c.GetCategoryAttributeDefinitionsAsync(category.Path))
            .ReturnsAsync(new List<AttributeDefinition>());

        _productManagementMapperMock.Setup(m => m.RequestToModel(requestDto, category, It.IsAny<List<Attribute>>()))
            .Returns(productModel);

        _productAttributesValidatorMock.Setup(v => v.Validate(productModel.Attributes, It.IsAny<IEnumerable<AttributeDefinition>>()))
            .Returns(new ValidationResult() { IsValid = true });

        _productManagementRepositoryMock.Setup(r => r.CreateAsync(productModel))
            .ReturnsAsync(createdId);

        _productEventProducerMock.Setup(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateProductAsync(requestDto);

        // Assert
        Assert.That(result, Is.EqualTo(createdId));

        _categoryRepositoryMock.Verify(c => c.GetByIdAsync(requestDto.CategoryId), Times.Once);
        _productManagementMapperMock.Verify(m => m.RequestToModel(requestDto, category, It.IsAny<List<Attribute>>()), Times.Once);
        _productAttributesValidatorMock.Verify(v => v.Validate(productModel.Attributes, It.IsAny<IEnumerable<AttributeDefinition>>()), Times.Once);
        _productManagementRepositoryMock.Verify(r => r.CreateAsync(productModel), Times.Once);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.Is<ProductEventDto>(e => e.ProductId == createdId && e.ProductName == productModel.Name), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreateProductAsync_ShouldThrowNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var requestDto = new CreateProductRequestDto("prod1", "sample", 2, new List<ProductAttributeRequest>(), nameof(Condition.New));

        _categoryRepositoryMock
            .Setup(c => c.GetByIdAsync(requestDto.CategoryId))
            .ReturnsAsync(default(Category.Category));

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.CreateProductAsync(requestDto));

        Assert.That(ex.Message, Is.EqualTo($"Category with id: {requestDto.CategoryId} does not exist"));

        _categoryRepositoryMock.Verify(c => c.GetByIdAsync(requestDto.CategoryId), Times.Once);
        _productManagementMapperMock.Verify(m => m.RequestToModel(It.IsAny<CreateProductRequestDto>(), It.IsAny<Category.Category>(), It.IsAny<List<Attribute>>()), Times.Never);
        _productAttributesValidatorMock.Verify(v => v.Validate(It.IsAny<IEnumerable<Attribute>>(), It.IsAny<IEnumerable<AttributeDefinition>>()), Times.Never);
        _productManagementRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Never);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateProductAsync_ValidationFails_ReturnsBadRequest()
    {
        // Arrange
        var requestDto = new CreateProductRequestDto("prod1", "sample", 2, new List<ProductAttributeRequest>(), nameof(Condition.New));

        var category = new Category.Category() { Id = 42, Path = "category/path" };
        var productModel = new Product { Name = requestDto.Name, Attributes = [] };

        _categoryRepositoryMock.Setup(c => c.GetByIdAsync(requestDto.CategoryId))
            .ReturnsAsync(category);

        _categoryRepositoryMock.Setup(c => c.GetCategoryAttributeDefinitionsAsync(category.Path))
            .ReturnsAsync(new List<AttributeDefinition>());

        _productManagementMapperMock.Setup(m => m.RequestToModel(requestDto, category, new List<Attribute>()))
            .Returns(productModel);

        _productAttributesValidatorMock.Setup(v => v.Validate(productModel.Attributes, It.IsAny<IEnumerable<AttributeDefinition>>()))
            .Returns(new ValidationResult() { IsValid = false, ErrorMessage = "Invalid attributes" });

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
            await _service.CreateProductAsync(requestDto));

        Assert.That(ex.Message, Is.EqualTo("Invalid attributes"));

        _productManagementRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Never);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
        _productManagementMapperMock.Verify(m => m.RequestToModel(It.IsAny<CreateProductRequestDto>(), It.IsAny<Category.Category>(), It.IsAny<List<Attribute>>()), Times.Once);
        _productAttributesValidatorMock.Verify(v => v.Validate(productModel.Attributes, It.IsAny<IEnumerable<AttributeDefinition>>()), Times.Once);
    }
    
    [Test]
    public async Task DeleteProductByIdAsync_ShouldDeleteAndProduceEvent_WhenProductExistsAndNotListed()
    {
        // Arrange
        var productId = "P123";
        var product = new Product { Id = productId, Name = "Test Product" };

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId))
            .ReturnsAsync(false);
        _productManagementRepositoryMock.Setup(r => r.DeleteByIdAsync(productId))
            .Returns(Task.CompletedTask);
        _productEventProducerMock.Setup(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteProductByIdAsync(productId);

        // Assert
        _productListingRepositoryMock.Verify(r => r.IsProductAlreadyListedAsync(productId), Times.Once);
        _productManagementRepositoryMock.Verify(r => r.DeleteByIdAsync(productId), Times.Once);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.Is<ProductEventDto>(
            e => e.ProductId == productId && e.ProductName == product.Name && e.EventType == nameof(Events.ProductDeleted)
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void DeleteProductByIdAsync_ShouldThrowBadRequest_WhenProductIsListed()
    {
        // Arrange
        var productId = "P123";
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
            await _service.DeleteProductByIdAsync(productId));

        Assert.That(ex.Message, Is.EqualTo("You cannot delete a product that is listed."));

        _productManagementRepositoryMock.Verify(r => r.DeleteByIdAsync(It.IsAny<string>()), Times.Never);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteProductByIdAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = "P123";
        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(default(Product));
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.DeleteProductByIdAsync(productId));

        Assert.That(ex.Message, Is.EqualTo($"Product with id: {productId} not found"));

        _productManagementRepositoryMock.Verify(r => r.DeleteByIdAsync(It.IsAny<string>()), Times.Never);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Test]
    public async Task UpdateProductAsync_ShouldCallUpdate_WhenAllValid()
    {
        // Arrange
        var productId = "P123";
        var requestDto = new CreateProductRequestDto("prod1", "sample", 2, new List<ProductAttributeRequest>(), nameof(Condition.New));

        var existingProduct = new Product { Id = productId, Name = "Old Name", Attributes = new List<Attribute>() };
        var category = new Category.Category { Id = requestDto.CategoryId, Path = "category/path" };
        var updatedProduct = new Product { Name = requestDto.Name, Attributes = new List<Attribute>() };

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(false);
        _categoryRepositoryMock.Setup(c => c.GetByIdAsync(requestDto.CategoryId)).ReturnsAsync(category);
        _categoryRepositoryMock.Setup(c => c.GetCategoryAttributeDefinitionsAsync(category.Path)).ReturnsAsync(new List<AttributeDefinition>());
        _productManagementMapperMock.Setup(m => m.RequestToModel(requestDto, category, new List<Attribute>())).Returns(updatedProduct);
        _productAttributesValidatorMock.Setup(v => v.Validate(updatedProduct.Attributes, It.IsAny<IEnumerable<AttributeDefinition>>()))
            .Returns(new ValidationResult { IsValid = true });
        _productManagementRepositoryMock.Setup(r => r.UpdateAsync(productId, updatedProduct)).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateProductAsync(productId, requestDto);

        // Assert
        _productManagementRepositoryMock.Verify(r => r.UpdateAsync(productId, updatedProduct), Times.Once);
        _productManagementMapperMock.Verify(m => m.RequestToModel(requestDto, category, It.IsAny<List<Attribute>>()), Times.Once);
        _productAttributesValidatorMock.Verify(v => v.Validate(updatedProduct.Attributes, It.IsAny<IEnumerable<AttributeDefinition>>()), Times.Once);
    }

    [Test]
    public void UpdateProductAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = "P123";
        var requestDto = new CreateProductRequestDto("prod1", "sample", 2, new List<ProductAttributeRequest>(), nameof(Condition.New));

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.UpdateProductAsync(productId, requestDto));

        Assert.That(ex.Message, Is.EqualTo($"Product with id: {productId} not found"));
    }

    [Test]
    public void UpdateProductAsync_WhenProductIsListed_ThrowsBadRequestException()
    {
        // Arrange
        var productId = "P123";
        var requestDto = new CreateProductRequestDto("prod1", "sample", 2, new List<ProductAttributeRequest>(), nameof(Condition.New));
        var existingProduct = new Product { Id = productId };

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(true);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
            await _service.UpdateProductAsync(productId, requestDto));

        Assert.That(ex.Message, Is.EqualTo("You cannot update a product that is listed."));
    }

    [Test]
    public void UpdateProductAsync_ShouldThrowNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var productId = "P123";
        var requestDto = new CreateProductRequestDto("prod1", "sample", 2, new List<ProductAttributeRequest>(), nameof(Condition.New));
        var existingProduct = new Product { Id = productId };

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(false);
        _categoryRepositoryMock.Setup(c => c.GetByIdAsync(requestDto.CategoryId)).ReturnsAsync(default(Category.Category));

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.UpdateProductAsync(productId, requestDto));

        Assert.That(ex.Message, Is.EqualTo($"Category with id: {requestDto.CategoryId} does not exist"));
    }

    [Test]
    public void UpdateProductAsync_ValidationFails_ThrowsBadRequestException()
    {
        // Arrange
        var productId = "P123";
        var requestDto = new CreateProductRequestDto("prod1", "sample", 2, new List<ProductAttributeRequest>(), nameof(Condition.New));
        var existingProduct = new Product { Id = productId };
        var category = new Category.Category { Id = requestDto.CategoryId, Path = "category/path" };
        var updatedProduct = new Product { Attributes = new List<Attribute>() };

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(false);
        _categoryRepositoryMock.Setup(c => c.GetByIdAsync(requestDto.CategoryId)).ReturnsAsync(category);
        _categoryRepositoryMock.Setup(c => c.GetCategoryAttributeDefinitionsAsync(category.Path)).ReturnsAsync(new List<AttributeDefinition>());
        _productManagementMapperMock.Setup(m => m.RequestToModel(requestDto, category, new List<Attribute>())).Returns(updatedProduct);
        _productAttributesValidatorMock.Setup(v => v.Validate(updatedProduct.Attributes, It.IsAny<IEnumerable<AttributeDefinition>>()))
            .Returns(new ValidationResult { IsValid = false, ErrorMessage = "Invalid attributes" });

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
            await _service.UpdateProductAsync(productId, requestDto));

        Assert.That(ex.Message, Is.EqualTo("Invalid attributes"));
    }
    
    [Test]
    public async Task AddMediaAsync_ShouldReturnProductMediaResponse_WhenAllValid()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var fileStream = new MemoryStream([1, 2, 3]);
        var file = new MockFormFile(fileStream, "test.jpg", "image/jpeg", "whatever");

        _productManagementRepositoryMock.Setup(r => r.ExistsByIdAsync(productId)).ReturnsAsync(true);

        _fileInspectorMock.Setup(f => f.Inspect(It.IsAny<IFormFile>())).Returns(new FileInfo());
        _fileValidatorMock.Setup(v => v.Validate(It.IsAny<FileConfig>(), It.IsAny<FileInfo>()))
            .Returns(new ValidationResult() { IsValid = true });

        _storageClientMock.Setup(s => s.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .Returns(Task.CompletedTask);

        _productMediaRepositoryMock.Setup(r => r.AddMediaAsync(It.IsAny<string>(), It.IsAny<ProductMedia>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddMediaAsync(productId, file, true);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Url, Does.StartWith("https://cdn.example.com"));
            Assert.That(result.IsPrimaryImage, Is.True);
        });

        _storageClientMock.Verify(s => s.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>()), Times.Once);
        _productMediaRepositoryMock.Verify(r => r.AddMediaAsync(productId, It.IsAny<ProductMedia>()), Times.Once);
    }
    
    [Test]
    public void AddMediaAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var file = new MockFormFile(new MemoryStream(new byte[] { 1, 2, 3 }), "test.jpg", "image/jpeg", "");

        _productManagementRepositoryMock.Setup(r => r.ExistsByIdAsync(productId)).ReturnsAsync(false);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.AddMediaAsync(productId, file, true));

        Assert.That(ex.Message, Is.EqualTo($"Product with id: {productId} not found"));
    }

    [Test]
    public void AddMediaAsync_ShouldThrowBadRequest_WhenFileInvalid()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var file = new MockFormFile(new MemoryStream(new byte[] { 1, 2, 3 }), "invalid.txt", "text/plain", "");

        _productManagementRepositoryMock.Setup(r => r.ExistsByIdAsync(productId)).ReturnsAsync(true);

        var fileInfo = new FileInfo { Extension = ".txt", Type = FileType.Document };
        _fileInspectorMock.Setup(f => f.Inspect(It.IsAny<IFormFile>())).Returns(fileInfo);
        _fileValidatorMock.Setup(v => v.Validate(It.IsAny<FileConfig>(), It.IsAny<FileInfo>()))
            .Returns(new ValidationResult { IsValid = false, ErrorMessage = "Invalid file type" });

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
            await _service.AddMediaAsync(productId, file, false));

        Assert.That(ex.Message, Is.EqualTo("Invalid file type"));
    }

    [Test]
    public async Task RemoveMediaAsync_ShouldRemoveMedia_WhenValid()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var mediaId = 1L;
        var media = new ProductMedia("file.jpg", "bucket", "key", Guid.NewGuid().ToString(), FileType.Image, false);

        _productManagementRepositoryMock.Setup(r => r.ExistsByIdAsync(productId)).ReturnsAsync(true);
        _productMediaRepositoryMock.Setup(r => r.GetProductMediaByIdAsync(productId, mediaId))
            .ReturnsAsync(media);
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(false);
        _storageClientMock.Setup(s => s.RemoveObjectAsync(media.Bucket, media.Key)).Returns(Task.CompletedTask);
        _productMediaRepositoryMock.Setup(r => r.RemoveMediaAsync(productId, mediaId)).Returns(Task.CompletedTask);

        // Act
        await _service.RemoveMediaAsync(productId, mediaId);

        // Assert
        _storageClientMock.Verify(s => s.RemoveObjectAsync(media.Bucket, media.Key), Times.Once);
        _productMediaRepositoryMock.Verify(r => r.RemoveMediaAsync(productId, mediaId), Times.Once);
    }

    [Test]
    public void RemoveMediaAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        const long mediaId = 1L;

        _productManagementRepositoryMock.Setup(r => r.ExistsByIdAsync(It.IsAny<string>())).ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.RemoveMediaAsync(productId, mediaId));
    }

    [Test]
    public void RemoveMediaAsync_ShouldThrowNotFound_WhenMediaDoesNotExist()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        const long mediaId = 1L;

        _productManagementRepositoryMock.Setup(r => r.ExistsByIdAsync(It.IsAny<string>())).ReturnsAsync(true);
        _productMediaRepositoryMock.Setup(r => r.GetProductMediaByIdAsync(It.IsAny<string>(), It.IsAny<long>()))
            .ReturnsAsync(default(ProductMedia));

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.RemoveMediaAsync(productId, mediaId));
    }

    [Test]
    public void RemoveMediaAsync_ShouldThrowBadRequest_WhenPrimaryImageOfListedProduct()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        const long mediaId = 1L;
        var media = new ProductMedia("file.jpg", "bucket", "key", Guid.NewGuid().ToString(), FileType.Image, true);

        _productManagementRepositoryMock.Setup(r => r.ExistsByIdAsync(It.IsAny<string>())).ReturnsAsync(true);
        _productMediaRepositoryMock.Setup(r => r.GetProductMediaByIdAsync(It.IsAny<string>(), It.IsAny<long>()))
            .ReturnsAsync(media);
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(true);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(async () =>
            await _service.RemoveMediaAsync(productId, mediaId));
    }
    
    [Test]
    public async Task GetProductListingHistoryAsync_ShouldReturnMappedResponses_WhenRepositoryReturnsData()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var product = new Product { Id = Guid.NewGuid().ToString(), Name = "Test Product", Description = "Sample description" };
        var listings = new List<ProductListing>
        {
            new ProductListing
            {
                Id = 1,
                Price = 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Product = product
            },
            new ProductListing
            {
                Id = 2,
                Price = 200,
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Product = product
            }
        };

        _productListingRepositoryMock.Setup(r => r.GetProductListingHistoryAsync(productId))
            .ReturnsAsync(listings);

        // Act
        var result = await _service.GetProductListingHistoryAsync(productId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(listings.Count));
        Assert.Multiple(() =>
        {
            Assert.That(result.First().Id, Is.EqualTo(listings[0].Id));
            Assert.That(result.First().PriceInEur, Is.EqualTo(listings[0].Price));
            Assert.That(result.First().ProductId, Is.EqualTo(listings[0].Product.Id.ToString()));
            Assert.That(result.First().ProductName, Is.EqualTo(listings[0].Product.Name));
        });
    }

    [Test]
    public async Task GetProductListingHistoryAsync_ShouldReturnNull_WhenRepositoryReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        _productListingRepositoryMock.Setup(r => r.GetProductListingHistoryAsync(productId))
            .ReturnsAsync(default(List<ProductListing>));

        // Act
        var result = await _service.GetProductListingHistoryAsync(productId);

        // Assert
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public async Task ListProductAsync_ShouldListProduct_WhenAllValid()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Product",
            Media = new List<ProductMedia>
            {
                new("file.jpg", "bucket", "key", Guid.NewGuid().ToString(), FileType.Image, true)
            },
            IsInStock = true
        };
        var request = new ProductListingRequest(productId, 100);

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(false);
        _productListingRepositoryMock.Setup(r => r.ListProductAsync(productId, It.IsAny<ProductListing>()))
            .Returns(Task.CompletedTask);
        _productEventProducerMock.Setup(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _service.ListProductAsync(request);

        // Assert
        _productListingRepositoryMock.Verify(r => r.ListProductAsync(productId, It.Is<ProductListing>(pl => pl.Price == request.PriceInEur)), Times.Once);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.Is<ProductEventDto>(e => e.ProductId == product.Id && e.ProductName == product.Name), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ListProductAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var request = new ProductListingRequest(productId, 100);

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(default(Product));

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.ListProductAsync(request));
        Assert.That(ex.Message, Is.EqualTo($"Product with id: {productId} not found"));
    }

    [Test]
    public void ListProductAsync_ShouldThrowBadRequest_WhenNotEnoughMedia()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var product = new Product { Id = Guid.NewGuid().ToString(), Name = "Test Product", Media = new List<ProductMedia>() };
        var request = new ProductListingRequest(productId, 100);

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(async () => await _service.ListProductAsync(request));
    }

    [Test]
    public void ListProductAsync_ShouldThrowConflict_WhenProductAlreadyListed()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Product",
            Media = [new ProductMedia("file.jpg", "bucket", "key", Guid.NewGuid().ToString(), FileType.Image, true)]
        };
        var request = new ProductListingRequest(productId, 100);

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(true);

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(async () => await _service.ListProductAsync(request));
    }
    
    [Test]
    public async Task DelistProductAsync_ShouldDelist_WhenProductIsListed()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();

        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(true);
        _productListingRepositoryMock.Setup(r => r.DelistProductAsync(productId)).Returns(Task.CompletedTask);
        _productEventProducerMock.Setup(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _service.DelistProductAsync(productId);

        // Assert
        _productListingRepositoryMock.Verify(r => r.DelistProductAsync(productId), Times.Once);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.Is<ProductEventDto>(e =>
            e.ProductId == productId &&
            e.EventType == nameof(Events.ProductDelisted)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void DelistProductAsync_ShouldThrowNotFound_WhenProductNotListed()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();

        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId)).ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.DelistProductAsync(productId));
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task IsProductListedAsync_ShouldReturnRepositoryResult(bool repositoryResult)
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        _productListingRepositoryMock.Setup(r => r.IsProductAlreadyListedAsync(productId))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _service.IsProductListedAsync(productId);

        // Assert
        Assert.That(result, Is.EqualTo(repositoryResult));
        _productListingRepositoryMock.Verify(r => r.IsProductAlreadyListedAsync(productId), Times.Once);
    }
    
    [Test]
    public async Task ApplyDiscountAsync_ShouldApplyDiscount_WhenValid()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var product = new Product { Name = "Test Product", IsInStock = true };
        var listing = new ProductListing(100) { Product = product };
        var discountRequest = new DiscountRequest { Percentage = 20 };

        _productListingRepositoryMock.Setup(r => r.GetActiveProductListingAsync(productId))
            .ReturnsAsync(listing);
        _productListingRepositoryMock.Setup(r => r.UpdateProductListingAsync(listing))
            .Returns(Task.CompletedTask);
        _productEventProducerMock.Setup(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ApplyDiscountAsync(productId, discountRequest);

        // Assert
        var expectedPrice = (decimal)(100 - (100 * 20 / 100.0));
        Assert.That(listing.DiscountedPrice, Is.EqualTo(expectedPrice));

        _productListingRepositoryMock.Verify(r => r.UpdateProductListingAsync(listing), Times.Once);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.Is<ProductEventDto>(e =>
            e.ProductId == productId &&
            e.ProductName == product.Name &&
            e.Price == listing.Price &&
            e.DiscountedPrice == expectedPrice &&
            e.IsDiscounted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ApplyDiscountAsync_ShouldThrowNotFound_WhenListingDoesNotExist()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var discountRequest = new DiscountRequest { Percentage = 20 };

        _productListingRepositoryMock.Setup(r => r.GetActiveProductListingAsync(productId))
            .ReturnsAsync(default(ProductListing));

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.ApplyDiscountAsync(productId, discountRequest));

        Assert.That(ex.Message, Is.EqualTo($"Product with id: {productId} not listed"));
    }

    [TestCase(0)]
    [TestCase(-5)]
    [TestCase(100)]
    [TestCase(120)]
    public void ApplyDiscountAsync_ShouldThrowBadRequest_WhenPercentageInvalid(decimal percentage)
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var product = new Product { Name = "Test Product" };
        var listing = new ProductListing(100) { Product = product };
        var discountRequest = new DiscountRequest { Percentage = percentage };

        _productListingRepositoryMock.Setup(r => r.GetActiveProductListingAsync(productId))
            .ReturnsAsync(listing);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
            await _service.ApplyDiscountAsync(productId, discountRequest));

        Assert.That(ex.Message, Is.EqualTo("Discount percentage must be between 0 and 100"));
    }
    
    [Test]
    public async Task UpdateStockAvailabilityAsync_ShouldUpdateStock_WhenValueChanges()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var product = new Product { Id = Guid.NewGuid().ToString(), Name = "Test Product", IsInStock = false  };
        var listing = new ProductListing(100) { Product = product };

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);
        _productManagementRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<string>(), product)).Returns(Task.CompletedTask);
        _productEventProducerMock.Setup(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateStockAvailabilityAsync(productId, true);

        // Assert
        Assert.That(listing.Product.IsInStock, Is.True);
        _productManagementRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Product>()), Times.Once);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.Is<ProductEventDto>(e =>
            e.ProductId == product.Id &&
            e.ProductName == product.Name &&
            e.IsAvailable == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateStockAvailabilityAsync_ShouldThrowNotFound_WhenListingDoesNotExist()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();

        _productListingRepositoryMock.Setup(r => r.GetActiveProductListingAsync(productId))
            .ReturnsAsync(default(ProductListing));

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _service.UpdateStockAvailabilityAsync(productId, true));

        Assert.That(ex.Message, Is.EqualTo($"Product with id: {productId} not found"));
    }

    [Test]
    public async Task UpdateStockAvailabilityAsync_ShouldDoNothing_WhenValueUnchanged()
    {
        // Arrange
        var productId = Guid.NewGuid().ToString();
        var product = new Product { Id = Guid.NewGuid().ToString(), Name = "Test Product", IsInStock = true  };

        _productManagementRepositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        await _service.UpdateStockAvailabilityAsync(productId, true);

        // Assert
        _productListingRepositoryMock.Verify(r => r.UpdateProductListingAsync(It.IsAny<ProductListing>()), Times.Never);
        _productEventProducerMock.Verify(p => p.ProduceAsync(It.IsAny<ProductEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
}