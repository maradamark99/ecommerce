namespace EcommerceLib.File;

public class FileConfig
{
    
    public HashSet<string> AllowedExtensions { get; set; } = new HashSet<string>();
    
    public long MaxFileSizeInBytes { get; set; } = 50 * 1024 * 1024;
    
}