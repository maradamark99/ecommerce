namespace Ecommerce.Common.File;

public interface IFileInspector
{
    FileInfo Inspect(IFormFile file);
}