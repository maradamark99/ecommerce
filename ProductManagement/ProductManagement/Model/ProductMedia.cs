using EcommerceLib.File;

namespace ProductManagement.Model;

public class ProductMedia
{
    
    public long Id { get; set; }
    
    public string Name { get; set; }
    
    public string Bucket { get; set; }
    
    public string Key { get; set; }
    
    public Product Product { get; set; }
    
    public string ProductId { get; set; }
    
    public FileType FileType { get; set; }
    
    public bool IsPrimaryImage { get; set; }

    public ProductMedia()
    {
        
    }

    public ProductMedia(string name, string bucket, string key, string productId, FileType fileType, bool isPrimaryImage)
    {
        IsPrimaryImage = isPrimaryImage;
        Name = name;
        Bucket = bucket;
        Key = key;
        ProductId = productId;
        FileType = fileType;
    }
}