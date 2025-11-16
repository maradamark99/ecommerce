using Ecommerce.Common.Data;
using Ecommerce.Common.Exception;
using Ecommerce.Common.Pagination;
using Ecommerce.Domain.ProductManagement.Category;
using Ecommerce.Domain.ProductManagement.Category.Contract;
using Moq;

using CategoryModel = Ecommerce.Domain.ProductManagement.Category.Category;

namespace Ecommerce.Tests.Category;

[TestFixture]   
public class CategoryServiceTest
{
    private CategoryService _categoryService;
    
    private Mock<ICategoryRepository> _categoryRepositoryMock;
    
    private Mock<ICategoryMapper> _categoryMapperMock; 
    
    [SetUp]
    public void SetUp()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _categoryMapperMock = new Mock<ICategoryMapper>();
        _categoryService = new CategoryService(_categoryMapperMock.Object, _categoryRepositoryMock.Object);
    }   
    
    [Test]
    public async Task CreateCategoryAsync_HappyCase()
    {
        // Arrange
        var categoryRequest = new CategoryRequest("Electronics", null,  []);
        var categoryModel = CategoryModel.Builder()
            .WithPublicName(categoryRequest.Name)
            .Build();
        const int expectedCategoryId = 1;

        _categoryRepositoryMock
            .Setup(x => x.ExistsByPath(It.IsAny<string>()))
            .ReturnsAsync(false);


        _categoryMapperMock
            .Setup(x => x.RequestToModel(categoryRequest, null))
            .Returns(categoryModel);

        _categoryRepositoryMock
            .Setup(x => x.CreateCategoryAsync(categoryModel))
            .ReturnsAsync(expectedCategoryId);

        // Act
        var categoryId = await _categoryService.CreateCategoryAsync(categoryRequest);

        // Assert
        Assert.That(categoryId, Is.EqualTo(expectedCategoryId));

        _categoryRepositoryMock.Verify(x => x.CreateCategoryAsync(It.IsAny<CategoryModel>()), Times.Once);
    }
    
    [Test] 
    public void CreateCategoryAsync_WhenParentCategoryDoesNotExist_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var categoryRequest = new CategoryRequest("Electronics", 1, []);
        
        _categoryRepositoryMock
            .Setup(x => x.ExistsByPath(It.IsAny<string>()))
            .ReturnsAsync(false);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryRequest.ParentId!.Value))
            .ReturnsAsync((CategoryModel?)null);

        // Act
        var exception = Assert.ThrowsAsync<NotFoundException>(() => _categoryService.CreateCategoryAsync(categoryRequest));

        // Assert
        Assert.That(exception.Message, Is.EqualTo($"Category with id: {categoryRequest.ParentId} not found"));
    }
   
    [Test]
    public async Task GetAllCategoriesAsync_WhenCalled_ShouldReturnCategories()
    {
        // Arrange
        var pager = new Pager();
        var sorter = new Sorter();
        var categories = Paged<CategoryModel>.Of(new List<CategoryModel>(), 1, 10, 0);
        var expected = Paged<CategoryResponse>.Of(new List<CategoryResponse>(), 1, 10, 0);

        _categoryRepositoryMock
            .Setup(x => x.GetAllCategoriesAsync(pager, sorter))
            .ReturnsAsync(categories);

        _categoryMapperMock
            .Setup(x => x.ModelToResponse(It.IsAny<CategoryModel>()))
            .Returns(new CategoryResponse(1, "Electronics", "", []));

        // Act
        var result = await _categoryService.GetAllCategoriesAsync(pager, sorter);

        // Assert
        Assert.That(expected.Items, Is.EqualTo(result.Items).AsCollection);
    }
    
    [Test]
    public void GetAllCategoriesAsync_WhenSorterColumnIsInvalid_ShouldThrowInvalidSorterColumnException()
    {
        // Arrange
        var pager = new Pager();
        var sorter = new Sorter
        {
            SortBy = "invalid"
        };

        // Act
        var exception = Assert.ThrowsAsync<BadRequestException>(() => _categoryService.GetAllCategoriesAsync(pager, sorter));

        // Assert
        Assert.That(exception.Message, Is.EqualTo("Invalid column name for sorting: invalid"));
    }   

}