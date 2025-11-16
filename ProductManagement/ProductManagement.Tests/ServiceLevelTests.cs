using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;
using EcommerceLib.File;
using EcommerceLib.Pagination;
using EcommerceLib.Storage;
using EcommerceLib.Testing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Category;
using ProductManagement.Contract;
using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;

namespace ProductManagement.Tests;

public class ServiceLevelTests
{
    private TestEnvironment<ITestEnvironmentMarker> _testEnvironment;
    private const string AdminUserId = "adminId";
    private const string ProductManagement = "api/v1/product-management";
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testEnvironment = TestEnvironment<ITestEnvironmentMarker>.Builder
            .WithDatabase<AppDbContext>()
            .WithMockServer()
            .WithProducer()
            .WithConsumer()
            .WithServiceOverride(c =>
            {
                c.AddSingleton<IStorageClient, MockStorageClient>();
            })
            .Build();
        await _testEnvironment.StartAsync();
    }
    
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _testEnvironment.DisposeAsync();
    }
    
    [SetUp]
    public async Task SetUp()
    {
        await _testEnvironment.ResetAsync();
    }

    [Test]
    public async Task GetAllProducts_ValidRequest_ReturnsPagedProducts()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId1 = Guid.NewGuid().ToString();
        var productId2 = Guid.NewGuid().ToString();

        var product1 = CreateProduct(
            id: productId1,
            name: "Product A"
        );

        var product2 = CreateProduct(
            id: productId2,
            name: "Product B"
        );

        db.Products.AddRange(product1, product2);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync(
            ProductManagement + "?sortBy=Name&sortDirection=asc&page=1&pageSize=10");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<Paged<ProductManagementResponse>>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Has.Count.EqualTo(2));
        Assert.That(result.Items.Select(p => p.Name), Does.Contain("Product A"));
        Assert.Multiple(() =>
        {
            Assert.That(result.Items.Select(p => p.Name), Does.Contain("Product B"));
            Assert.That(result.Items.Select(p => p.Id), Does.Contain(productId1));
        });
        Assert.That(result.Items.Select(p => p.Id), Does.Contain(productId2));
    }
    
    [Test]
    public async Task GetProductById_ProductExists_ReturnsProduct()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var product = CreateProduct(
            id: productId,
            name: "Test Product"
        );

        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync(ProductManagement + $"/{productId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<ProductManagementResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(productId));
            Assert.That(result.Name, Is.EqualTo("Test Product"));
        });
    }

    [Test]
    public async Task GetProductById_ProductDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await client.GetAsync(ProductManagement + $"/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task CreateProduct_ValidRequest_ShouldCreateProductAndProduceEvent()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics"
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var request = new CreateProductRequestDto("New Product",
            "A test product",
            category.Id,
            [
                new ProductAttributeRequest("Color", "Red"),
                new ProductAttributeRequest("Size", "Medium")
            ],
            "New"
        );

        // Act
        var response = await client.PostAsJsonAsync(ProductManagement, request);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var createdProductId = content?["id"];

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createdProductId, Is.Not.Null);
            Assert.That(response.Headers.Location?.AbsoluteUri, Does.Contain($"/{createdProductId}"));
        });

        var createdProduct = await db.Products
            .Include(p => p.Attributes)
            .Include(p => p.Category).Include(product => product.Attributes)
            .FirstOrDefaultAsync(p => p.Id == createdProductId);

        Assert.Multiple(() =>
        {
            Assert.That(createdProduct, Is.Not.Null);
            Assert.That(createdProduct!.Name, Is.EqualTo(request.Name));
            Assert.That(createdProduct.Category.Id, Is.EqualTo(request.CategoryId));
            Assert.That(createdProduct.Attributes.Select(a => a.Name), Does.Contain("Color"));
            Assert.That(createdProduct.Attributes.Select(a => a.Value), Does.Contain("Red"));
        });

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<ProductEventDto>(Topics.ProductEvents, TimeSpan.FromSeconds(1));
        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<ProductEventDto>(e =>
            e.EventType == nameof(Events.ProductCreated) &&
            e.ProductId == createdProductId &&
            e.ProductName == request.Name), Is.True);
    }
    
    [Test]
    public async Task CreateProduct_InvalidCondition_ShouldReturnBadRequest()
    {
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var request = new CreateProductRequestDto(
            "New Product",
            "A test product",
            1,
            [
                new ProductAttributeRequest("Color", "Red"),
                new ProductAttributeRequest("Size", "Medium")
            ],            
            "InvalidCondition"
        );

        var response = await client.PostAsJsonAsync(ProductManagement, request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateProduct_NonExistentCategory_ShouldReturnNotFound()
    {
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var request = new CreateProductRequestDto(
            "New Product",
            "A test product",
            999,
            [
                new ProductAttributeRequest("Color", "Red"),
                new ProductAttributeRequest("Size", "Medium")
                ],     
            "New"
        );

        var response = await client.PostAsJsonAsync(ProductManagement, request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateProduct_InvalidAttributes_ShouldReturnBadRequest()
    {
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics",
            AttributesDefinitions =
            [
                new AttributeDefinition
                {
                    Id = 1,
                    Name = "Color",
                    Type = AttributeType.STRING,
                    IsRequired = true,
                    CategoryId = 1
                },

                new AttributeDefinition
                {
                    Id = 2,
                    Name = "Size",
                    Type = AttributeType.STRING,
                    IsRequired = false,
                    CategoryId = 1
                }
            ]
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var request = new CreateProductRequestDto(
            "New Product",
            "A test product",
            category.Id,
            [new ProductAttributeRequest("InvalidAttr", "Value")],
            "New"
        );

        var response = await client.PostAsJsonAsync(ProductManagement, request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task CreateProduct_SomeAttributesExist_ShouldCreateProduct()
    {
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics",
            AttributesDefinitions =
            [
                new AttributeDefinition
                {
                    Id = 1,
                    Name = "Color",
                    Type = AttributeType.STRING,
                    IsRequired = true,
                    CategoryId = 1
                },

                new AttributeDefinition
                {
                    Id = 2,
                    Name = "Size",
                    Type = AttributeType.STRING,
                    IsRequired = true,
                    CategoryId = 1
                }
            ]
        };
        db.ProductAttributes.Add(new Attribute { Name = "Color", Value = "Black" });
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var request = new CreateProductRequestDto(
            "New Product",
            "A test product",
            category.Id,
            [new ProductAttributeRequest("Color", "Black"), new ProductAttributeRequest("Size", "XL") ],
            "New"
        );

        var response = await client.PostAsJsonAsync(ProductManagement, request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(db.ProductAttributes.Count(p => p.Name == "Color" && p.Value == "Black"), Is.EqualTo(1));
        Assert.That(db.ProductAttributes.Count(p => p.Name == "Size" && p.Value == "XL"), Is.EqualTo(1));
    }
    
    [Test]
    public async Task DeleteProduct_ProductExistsAndNotListed_ShouldDeleteAndProduceEvent()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Test Description",
            Condition = Condition.New,
            Category = new ProductManagement.Category.Category
            {
                Id = 1,
                PublicName = "Electronics",
                Slug = "electronics",
                Path = "electronics"
            },
            Attributes = []
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Act
        var response = await client.DeleteAsync($"{ProductManagement}/{productId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var deletedProduct = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
        Assert.That(deletedProduct, Is.Null);

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<ProductEventDto>(
            Topics.ProductEvents, TimeSpan.FromSeconds(1));

        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<ProductEventDto>(e =>
            e.EventType == nameof(Events.ProductDeleted) && e.ProductId == productId), Is.True);
    }

    [Test]
    public async Task DeleteProduct_ProductDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await client.DeleteAsync($"{ProductManagement}/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteProduct_ProductIsListed_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Listed Product",
            Description = "Test",
            Condition = Condition.New,
            Category = new ProductManagement.Category.Category { Id = 1, PublicName = "Electronics", Path = "electronics", Slug = "electronics" },
            Attributes = [],
            IsInStock = true
        };
        db.Products.Add(product);

        db.ProductListings.Add(new ProductListing
        {
            Product = product,
            Price = 100m,
            IsActive = true,
        });

        await db.SaveChangesAsync();

        // Act
        var response = await client.DeleteAsync($"{ProductManagement}/{productId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task UpdateProduct_ProductExistsAndNotListed_ShouldUpdateSuccessfully()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Path = "electronics",
            Slug = "electronics",
            AttributesDefinitions =
            [
                new AttributeDefinition { Id = 1, Name = "Color", Type = AttributeType.STRING, IsRequired = true, CategoryId = 1 },
                new AttributeDefinition { Id = 2, Name = "Size", Type = AttributeType.STRING, IsRequired = false, CategoryId = 1 }
            ]
        };
        db.Categories.Add(category);

        var productId = Guid.NewGuid().ToString();
        var product = new Product()
        {
            Id = productId,
            Name = "Old Product",
            Description = "Old Description",
            Condition = Condition.New,
            Category = category,
            Attributes = 
            [
                Attribute.Builder().WithName("Color").WithValue("Red").Build(),
                Attribute.Builder().WithName("Size").WithValue("Medium").Build()
            ]
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var updateRequest = new CreateProductRequestDto(
            "Updated Product",
            "Updated Description",
            category.Id,
            [
                new ProductAttributeRequest("Color", "Blue"),
                new ProductAttributeRequest("Size", "Large")
            ],
            "New"
        );

        // Act
        var response = await client.PutAsJsonAsync($"{ProductManagement}/{productId}", updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var updatedProduct = await db.Products.AsNoTracking().Include(p => p.Attributes).FirstOrDefaultAsync(p => p.Id == productId);
        Assert.That(updatedProduct, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updatedProduct.Name, Is.EqualTo("Updated Product"));
            Assert.That(updatedProduct.Description, Is.EqualTo("Updated Description"));
            Assert.That(updatedProduct.Attributes.First(a => a.Name == "Color").Value, Is.EqualTo("Blue"));
            Assert.That(updatedProduct.Attributes.First(a => a.Name == "Size").Value, Is.EqualTo("Large"));
        });
    }
    
    [Test]
    public async Task UpdateProduct_ProductDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var nonExistentId = Guid.NewGuid().ToString();
        var updateRequest = new CreateProductRequestDto(
            "Updated Product",
            "Updated Description",
            1,
            [],
            "New"
        );

        // Act
        var response = await client.PutAsJsonAsync($"{ProductManagement}/{nonExistentId}", updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task UpdateProduct_ProductIsListed_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Path = "electronics",
            Slug = "electronics"
        };
        db.Categories.Add(category);

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Listed Product",
            Description = "Description",
            Condition = Condition.New,
            Category = category,
            IsInStock = true,
            Attributes = []
        };
        db.Products.Add(product);

        db.ProductListings.Add(new ProductListing
        {
            Product = product,
            Price = 100m,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var updateRequest = new CreateProductRequestDto(
            "Updated Product",
            "Updated Description",
            category.Id,
            [],
            "New"
        );

        // Act
        var response = await client.PutAsJsonAsync($"{ProductManagement}/{productId}", updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UpdateProduct_InvalidAttributes_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Path = "electronics",
            Slug = "electronics",
            AttributesDefinitions = new List<AttributeDefinition>
            {
                new() { Id = 1, Name = "Color", Type = AttributeType.STRING, IsRequired = true, CategoryId = 1 }
            }
        };
        db.Categories.Add(category);

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Description",
            Condition = Condition.New,
            Category = category,
            Attributes = [Attribute.Builder().WithName("Color").WithValue("Red").Build()]
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var updateRequest = new CreateProductRequestDto(
            "Updated Product",
            "Updated Description",
            category.Id,
            [],
            "New"
        );

        // Act
        var response = await client.PutAsJsonAsync($"{ProductManagement}/{productId}", updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task UpdateProduct_AttributeExists_HappyCase()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Path = "electronics",
            Slug = "electronics",
            AttributesDefinitions = new List<AttributeDefinition>
            {
                new() { Id = 1, Name = "Color", Type = AttributeType.STRING, IsRequired = true, CategoryId = 1 },
                new() { Id = 2, Name = "Num", Type = AttributeType.NUMERIC, IsRequired = true, CategoryId = 1 }
            }
        };
        db.Categories.Add(category);

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Description",
            Condition = Condition.New,
            Category = category,
            Attributes = [
                Attribute.Builder().WithName("Color").WithValue("Red").Build(),
                Attribute.Builder().WithName("Num").WithValue("2").Build()
            ]
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var updateRequest = new CreateProductRequestDto(
            "Updated Product",
            "Updated Description",
            category.Id,
            [new ProductAttributeRequest("Color", "Red"), new ProductAttributeRequest("Num", "5")],
            "New"
        );

        // Act
        var response = await client.PutAsJsonAsync($"{ProductManagement}/{productId}", updateRequest);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(db.ProductAttributes.Count(a => a.Name == "Color" && a.Value == "Red"), Is.EqualTo(1));
            Assert.That(db.ProductAttributes.Count(a => a.Name == "Num" && a.Value == "2"), Is.EqualTo(1));
            Assert.That(db.ProductAttributes.Count(a => a.Name == "Num" && a.Value == "5"), Is.EqualTo(1));
        });
        var updatedProduct = db.Products.AsNoTracking().Include(p => p.Attributes).First(p => p.Id == productId);
        Assert.That(updatedProduct.Attributes.Count, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(updatedProduct.Attributes.Any(a => a.Name == "Color" && a.Value == "Red"), Is.True);
            Assert.That(updatedProduct.Attributes.Any(a => a.Name == "Num" && a.Value == "5"), Is.True);
        });
    }
    
    [Test]
    public async Task AddMediaAsync_ValidFile_ShouldReturnMediaResponse()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Path = "electronics",
            Slug = "electronics",
            AttributesDefinitions = new List<AttributeDefinition>
            {
                new() { Id = 1, Name = "Color", Type = AttributeType.STRING, IsRequired = true, CategoryId = 1 }
            }
        };
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Description",
            Condition = Condition.New,
            Category = category,
            Attributes = [Attribute.Builder().WithName("Color").WithValue("Red").Build()]
        };
        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var fileContent = new byte[] { 1, 2, 3 };
        using var ms = new MemoryStream(fileContent);
        var fileContentBytes = new ByteArrayContent(fileContent);
        fileContentBytes.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

        using var form = new MultipartFormDataContent();
        form.Add(fileContentBytes, "file", "image.jpg");

        // Act
        var response = await client.PostAsync($"{ProductManagement}/media/{productId}/?isPrimaryImage=true", form);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var mediaResponse = await response.Content.ReadFromJsonAsync<ProductMediaResponse>();
        Assert.That(mediaResponse, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(mediaResponse.FileType, Is.EqualTo(FileType.Image));
            Assert.That(mediaResponse.IsPrimaryImage, Is.True);
            Assert.That(mediaResponse.Url, Is.Not.Empty);
        });
    }

    [Test]
    public async Task AddMediaAsync_ProductDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var fakeProductId = Guid.NewGuid().ToString();
        var fileContent = new byte[] { 1, 2, 3 };
        using var ms = new MemoryStream(fileContent);
        var fileContentBytes = new ByteArrayContent(fileContent);
        fileContentBytes.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

        using var form = new MultipartFormDataContent();
        form.Add(fileContentBytes, "file", "image.jpg");
        // Act
        var response = await client.PostAsync($"{ProductManagement}/media/{fakeProductId}?isPrimaryImage=false", form);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AddMediaAsync_InvalidFile_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        db.Products.Add(new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Description",
            Condition = Condition.New,
            Category = new Category.Category
            {
            Id = 1,
            PublicName = "Electronics",
            Path = "electronics",
            Slug = "electronics",
        }});
        await db.SaveChangesAsync();

        var fileContent = Array.Empty<byte>();
        using var ms = new MemoryStream(fileContent);
        var fileContentBytes = new ByteArrayContent(fileContent);
        fileContentBytes.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

        using var form = new MultipartFormDataContent();
        form.Add(fileContentBytes, "file", "image.jpg");

        // Act
        var response = await client.PostAsync($"{ProductManagement}/media/{productId}?isPrimaryImage=false", form);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task DeleteMediaAsync_ValidMedia_ShouldReturnNoContent()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Path = "electronics",
            Slug = "electronics"
        };
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Description",
            Condition = Condition.New,
            Category = category,
            Attributes = []
        };
        db.Categories.Add(category);
        db.Products.Add(product);

        var media = new ProductMedia("image.jpg", Buckets.ProductMedia, "key123", productId, FileType.Image, isPrimaryImage: false);
        db.ProductMedia.Add(media);

        await db.SaveChangesAsync();

        // Act
        var response = await client.DeleteAsync($"{ProductManagement}/media/{productId}/{media.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var deletedMedia = await db.ProductMedia
            .FirstOrDefaultAsync(m => m.Id == media.Id && m.ProductId == productId);

        Assert.That(deletedMedia, Is.Null);
    }
    
    [Test]
    public async Task RemoveMedia_ProductDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var nonExistentProductId = Guid.NewGuid().ToString();

        // Act
        var response = await client.DeleteAsync($"{ProductManagement}/{nonExistentProductId}/1");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task RemoveMedia_MediaDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Path = "electronics",
            Slug = "electronics"
        };
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Description",
            Condition = Condition.New,
            Category = category,
            Attributes = []
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Act
        var response = await client.DeleteAsync($"{ProductManagement}/{productId}/999");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task RemoveMedia_PrimaryImageOfListedProduct_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Path = "electronics",
            Slug = "electronics"
        };
        db.Categories.Add(category);

        var product = new Product
        {
            Id = productId,
            Name = "Listed Product",
            Description = "Description",
            Condition = Condition.New,
            Category = category,
            IsInStock = true,
            Attributes = []
        };
        db.Products.Add(product);

        db.ProductListings.Add(new ProductListing
        {
            Product = product,
            Price = 100m,
            IsActive = true
        });

        var primaryMedia = new ProductMedia("primary.jpg", Buckets.ProductMedia, "key-primary", productId, FileType.Image, true);
        db.ProductMedia.Add(primaryMedia);

        await db.SaveChangesAsync();

        // Act
        var response = await client.DeleteAsync($"{ProductManagement}/media/{productId}/{primaryMedia.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task GetProductListingHistory_ProductExistsWithListings_ReturnsListings()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Description",
            Condition = Condition.New,
            Attributes = [],
            Category =  new ProductManagement.Category.Category
            { 
                Id = 1,
                PublicName = "Electronics",
                Path = "electronics",
                Slug = "electronics" 
            }
        };

        db.Products.Add(product);

        var listing1 = new ProductListing
        {
            Id = 1,
            Product = product,
            Price = 100m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
        };

        var listing2 = new ProductListing
        {
            Id = 2,
            Product = product,
            Price = 90m,
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        };

        db.ProductListings.AddRange(listing1, listing2);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"{ProductManagement}/listings/{productId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<List<ProductListingResponse>>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result!.Select(r => r.Id), Does.Contain(listing1.Id));
            Assert.That(result.Select(r => r.Id), Does.Contain(listing2.Id));
            Assert.That(result.Select(r => r.PriceInEur), Does.Contain(100m));
        });
        Assert.That(result.Select(r => r.PriceInEur), Does.Contain(90m));
    }

    [Test]
    public async Task GetProductListingHistory_ProductExistsWithoutListings_ReturnsEmpty()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Description",
            Condition = Condition.New,
            Attributes = [],
            Category =  new ProductManagement.Category.Category
            { 
                Id = 1,
                PublicName = "Electronics",
                Path = "electronics",
                Slug = "electronics" 
            }
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"{ProductManagement}/listings/{productId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<List<ProductListingResponse>>();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetProductListingHistory_ProductDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var nonExistentProductId = Guid.NewGuid().ToString();

        // Act
        var response = await client.GetAsync($"{ProductManagement}/listings/{nonExistentProductId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }
    
    [Test]
    public async Task ListProduct_ValidProduct_ShouldListAndProduceEvent()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics",
        };
        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "A test product",
            Condition = Condition.New,
            Category = category,
            IsInStock = true,
            Media = [new ProductMedia("img.jpg", Buckets.ProductMedia, "key1", productId, FileType.Image, true)]
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var request = new ProductListingRequest(productId, 99.99m);

        // Act
        var response = await client.PostAsJsonAsync($"{ProductManagement}/listings", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var listingInDb = await db.ProductListings
            .Include(p => p.Product)
            .Where(l => l.Product.Id == productId)
            .FirstOrDefaultAsync();

        Assert.That(listingInDb, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(listingInDb.Price, Is.EqualTo(request.PriceInEur));
            Assert.That(listingInDb.IsActive, Is.True);
        });

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<ProductEventDto>(
            Topics.ProductEvents, TimeSpan.FromSeconds(1));

        Assert.That(_testEnvironment.EventConsumer.HasMessageContaining<ProductEventDto>(
            e => e.EventType == nameof(Events.ProductListed) && e.ProductId == productId), Is.True);
    }
    
    [Test]
    public async Task ListProduct_ProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var request = new ProductListingRequest(Guid.NewGuid().ToString(), 99.99m);

        // Act
        var response = await client.PostAsJsonAsync($"{ProductManagement}/listings", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ListProduct_AlreadyListed_ShouldReturnConflict()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics",
        };

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "A test product",
            Condition = Condition.New,
            Category = category,
            Media = [new ProductMedia("img.jpg", Buckets.ProductMedia, "key1", productId, FileType.Image, true)]
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.ProductListings.Add(new ProductListing(99.99m) { Product = product, IsActive = true });
        await db.SaveChangesAsync();

        var request = new ProductListingRequest(productId, 99.99m);

        // Act
        var response = await client.PostAsJsonAsync($"{ProductManagement}/listings", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }
    
    [Test]
    public async Task DelistProduct_ValidListedProduct_ShouldMarkInactiveAndProduceEvent()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics",
        };

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "A test product",
            Condition = Condition.New,
            Category = category,
            Media = new List<ProductMedia>
            {
                new("img.jpg", Buckets.ProductMedia, "key1", productId, FileType.Image, true)
            }
        };

        var listing = new ProductListing(99.99m)
        {
            Product = product,
            IsActive = true
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.ProductListings.Add(listing);
        await db.SaveChangesAsync();

        // Act
        var response = await client.DeleteAsync($"{ProductManagement}/listings/{productId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var listingInDb = await db.ProductListings
            .AsNoTracking().FirstOrDefaultAsync(l => l.Product.Id == productId);

        Assert.That(listingInDb, Is.Not.Null);
        Assert.That(listingInDb.IsActive, Is.False);

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<ProductEventDto>(
            Topics.ProductEvents, TimeSpan.FromSeconds(1));

        Assert.That(_testEnvironment.EventConsumer.HasMessageContaining<ProductEventDto>(
            e => e.EventType == nameof(Events.ProductDelisted) && e.ProductId == productId), Is.True);
    }
    
    [Test]
    public async Task DelistProduct_ProductNotListed_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var productId = Guid.NewGuid().ToString();

        // Act
        var response = await client.DeleteAsync($"{ProductManagement}/listings/{productId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task ApplyDiscount_ValidListing_ShouldUpdatePriceAndProduceEvent()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics",
        };

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Discountable Product",
            Description = "A product for discount testing",
            Condition = Condition.New,
            Category = category,
            Media = [new ProductMedia("img.jpg", Buckets.ProductMedia, "key1", productId, FileType.Image, true)]
        };

        var listing = new ProductListing(100m)
        {
            Product = product,
            IsActive = true
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.ProductListings.Add(listing);
        await db.SaveChangesAsync();

        var discountRequest = new DiscountRequest
        {
            Percentage = 10m,
            ValidUntil = DateTimeOffset.UtcNow.AddDays(7)
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"{ProductManagement}/listings/{productId}/discount", discountRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var listingInDb = await db.ProductListings.AsNoTracking().FirstOrDefaultAsync(l => l.Product.Id == productId);
        Assert.That(listingInDb, Is.Not.Null);
        Assert.That(listingInDb.DiscountedPrice, Is.EqualTo(90m));

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<ProductEventDto>(
            Topics.ProductEvents, TimeSpan.FromSeconds(1));

        Assert.That(_testEnvironment.EventConsumer.HasMessageContaining<ProductEventDto>(
            e => e.EventType == nameof(Events.ProductListingUpdated) 
                 && e.ProductId == productId
                 && e is { DiscountedPrice: 90m, IsDiscounted: true }), Is.True);
    }
    
    
    [Test]
    public async Task ApplyDiscount_ProductNotListed_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        var productId = Guid.NewGuid().ToString();
        var discountRequest = new DiscountRequest
        {
            Percentage = 10m,
            ValidUntil = DateTimeOffset.UtcNow.AddDays(7)
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"{ProductManagement}/listings/{productId}/discount", discountRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase(0)]
    [TestCase(-5)]
    [TestCase(100)]
    [TestCase(150)]
    public async Task ApplyDiscount_InvalidPercentage_ShouldReturnBadRequest(decimal percentage)
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics",
        };

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Discountable Product",
            Description = "A product for discount testing",
            Condition = Condition.New,
            Category = category
        };

        var listing = new ProductListing(100m)
        {
            Product = product,
            IsActive = true
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.ProductListings.Add(listing);
        await db.SaveChangesAsync();

        var discountRequest = new DiscountRequest
        {
            Percentage = percentage,
            ValidUntil = DateTimeOffset.UtcNow.AddDays(7)
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"{ProductManagement}/listings/{productId}/discount", discountRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task InventoryEvent_InStock_ShouldUpdateStockAndProduceEvent()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(AdminUserId)
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics"
        };

        var productId = Guid.NewGuid().ToString();
        var product = new Product
        {
            Id = productId,
            Name = "Inventory Product",
            Description = "Product for inventory testing",
            Condition = Condition.New,
            IsInStock = false,
            Category = category
        };

        var listing = new ProductListing(50m)
        {
            Product = product,
            IsActive = true,
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.ProductListings.Add(listing);
        await db.SaveChangesAsync();

        var inventoryEvent = new InventoryEventDto
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.InventoryInStock),
            CorrelationId = Guid.NewGuid().ToString(),
            ProductId = productId,
            Quantity = 10,
            Timestamp = DateTime.UtcNow
        };

        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.InventoryEvents,
            inventoryEvent,
            Guid.NewGuid().ToString()
        );

        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert
        var listingInDb = await db.ProductListings.AsNoTracking().Include(productListing => productListing.Product).FirstOrDefaultAsync(l => l.Product.Id == productId);
        Assert.That(listingInDb, Is.Not.Null);
        Assert.That(listingInDb.Product.IsInStock, Is.True);

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<ProductEventDto>(
            Topics.ProductEvents, TimeSpan.FromSeconds(1));

        Assert.That(_testEnvironment.EventConsumer.HasMessageContaining<ProductEventDto>(
            e => e.EventType == nameof(Events.ProductListingUpdated)
                 && e.ProductId == productId
                 && e.IsAvailable), Is.True);
    }

    [Test]
    public async Task InventoryEvent_OutOfStock_ShouldUpdateStockAndProduceEvent()
    {
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productId = Guid.NewGuid().ToString();
        var category = new ProductManagement.Category.Category
        {
            Id = 1,
            PublicName = "Electronics",
            Slug = "electronics",
            Path = "electronics"
        };
        var product = new Product
        {
            Id = productId,
            Name = "Inventory Product",
            Description = "Product for inventory testing",
            Condition = Condition.New,
            Category = category,
            IsInStock = true

        };

        var listing = new ProductListing(50m)
        {
            Product = product,
            IsActive = true,
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.ProductListings.Add(listing);
        await db.SaveChangesAsync();

        var inventoryEvent = new InventoryEventDto
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.InventoryOutOfStock),
            CorrelationId = Guid.NewGuid().ToString(),
            ProductId = productId,
            Quantity = 0,
            Timestamp = DateTime.UtcNow
        };

        await _testEnvironment.EventProducer!.ProduceAsync(Topics.InventoryEvents, inventoryEvent, Guid.NewGuid().ToString());
        await Task.Delay(TimeSpan.FromSeconds(1));

        var listingInDb = await db.ProductListings.AsNoTracking().Include(productListing => productListing.Product).FirstOrDefaultAsync(l => l.Product.Id == productId);
        Assert.That(listingInDb, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(listingInDb.Product.IsInStock, Is.False);
            _testEnvironment.EventConsumer!.ConsumeWithTimeout<ProductEventDto>(
                Topics.ProductEvents, TimeSpan.FromSeconds(1));
            Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<ProductEventDto>(
                e => e.EventType == nameof(Events.ProductListingUpdated) && e.ProductId == productId && !e.IsAvailable), Is.True);
        });
    }

    private static Product CreateProduct(
        string? id = null,
        string name = "Test Product",
        string description = "This is a test product",
        Condition condition = Condition.New,
        string categoryName = "Electronics",
        int attributeCount = 2,
        bool includeMedia = false)
    {
        var category = new Category.Category()
        {
            PublicName = categoryName,
            Slug = categoryName.ToLowerInvariant().Replace(" ", "-"),
            Path = $"/{categoryName.ToLowerInvariant()}"
        };

        var product = new Product
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Condition = condition,
            Category = category,
            Attributes = []
        };

        for (var i = 1; i <= attributeCount; i++)
        {
            var attribute = Attribute.Builder()
                .WithName($"Attribute-{i}")
                .WithValue($"Value-{i}")
                .Build();

            attribute.Products = [product];

            product.Attributes.Add(attribute);
        }


        if (includeMedia)
        {
            product.Media =
            [
                new ProductMedia
                {
                    Bucket = "test-bucket",
                    Key = $"images/{product.Id}/primary.jpg",
                    FileType = FileType.Image,
                    IsPrimaryImage = true,
                    ProductId = product.Id
                }
            ];
        }

        return product;
    }
}