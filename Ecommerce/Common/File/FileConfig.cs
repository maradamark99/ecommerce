namespace Ecommerce.Common.File;

public class FileConfig
{
    
    public HashSet<string> AllowedExtensions { get; set; } = [];
    
    public long MaxFileSizeInBytes { get; set; } = 50 * 1024 * 1024;
    
}