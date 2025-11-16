using Microsoft.AspNetCore.Http;

namespace EcommerceLib.File;

public class NaiveFileInspector : IFileInspector
{
    public FileInfo Inspect(IFormFile file)
    {
        var fileExtension = Path.GetExtension(file.FileName);
        var fileType = Enum.Parse<FileType>(file.ContentType.Split("/").FirstOrDefault("UNKNOWN"), ignoreCase: true);
        return new FileInfo()
        {
            Extension = fileExtension,
            Length = file.Length,
            Type = fileType
        };
    }
}