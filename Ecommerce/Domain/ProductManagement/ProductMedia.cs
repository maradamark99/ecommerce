using Ecommerce.Common.File;

namespace Ecommerce.Domain.ProductManagement;

public class ProductMedia
{
    
    public long Id { get; set; }
    
    public string Name { get; set; }
    
    public string Bucket { get; set; }
    
    public string Key { get; set; }
    
    public Product Product { get; set; }
    
    public Guid ProductId { get; set; }
    
    public FileType FileType { get; set; }
    
    public bool IsPrimaryImage { get; set; }

    public ProductMedia()
    {
        
    }

    public ProductMedia(string name, string bucket, string key, Guid productId, FileType fileType, bool isPrimaryImage)
    {
        IsPrimaryImage = isPrimaryImage;
        Name = name;
        Bucket = bucket;
        Key = key;
        ProductId = productId;
        FileType = fileType;
    }
}