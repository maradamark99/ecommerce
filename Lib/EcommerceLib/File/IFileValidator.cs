namespace EcommerceLib.File;

public interface IFileValidator
{
    ValidationResult Validate(FileConfig config, FileInfo fileInfo);
}