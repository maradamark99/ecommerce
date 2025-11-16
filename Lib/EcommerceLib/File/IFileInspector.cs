using Microsoft.AspNetCore.Http;

namespace EcommerceLib.File;

public interface IFileInspector
{
    FileInfo Inspect(IFormFile file);
}