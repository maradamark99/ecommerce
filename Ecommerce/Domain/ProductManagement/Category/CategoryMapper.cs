using Ecommerce.Domain.ProductManagement.Category.Contract;

namespace Ecommerce.Domain.ProductManagement.Category;

public class CategoryMapper : ICategoryMapper
{
    
    public Category RequestToModel(CategoryRequest request, Category? parent)
    {
        var attrs = request.AttributeDefinitions?
            .Select(attr => AttributeDefinition.Builder()
                .WithName(attr.Name)
                .WithType(Enum.Parse<AttributeType>(attr.Type.ToString().ToUpper()))
                .WithIsRequired(attr.IsRequired)
                .Build())
            .ToList();
        var slug = CreateSlug(request.Name);
        return Category.Builder()
            .WithPublicName(request.Name)
            .WithSlug(slug)
            .WithPath(BuildCategoryPath(parent, slug))
            .WithAttributeDefinitions(attrs)
            .Build();
    }

    public CategoryResponse ModelToResponse(Category model)
    {
        var attrs = model.AttributesDefinitions?
            .Select(attr => new AttributeDefinitionResponse(attr.Name, attr.Type.ToString(), attr.IsRequired))
            .ToArray();
        return new CategoryResponse(model.Id, model.PublicName, model.Path, attrs);
    }
    
    private static string CreateSlug(string name)
    {
        return name.ToLowerInvariant().Replace(" ", "-").Replace("'", string.Empty);
    }
    
    private static string BuildCategoryPath(Category? parent, string slug)
    {
        return $"{(parent != null ? parent.Path + "/" : string.Empty)}{slug}";
    }
}