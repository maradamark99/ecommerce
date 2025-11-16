namespace Ecommerce.Common.File;

public interface IFileValidator
{
    ValidationResult Validate(in FileConfig config, FileInfo fileInfo);
}