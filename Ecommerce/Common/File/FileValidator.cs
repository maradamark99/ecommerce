namespace Ecommerce.Common.File;

public class FileValidator : IFileValidator 
{
    public ValidationResult Validate(in FileConfig config, FileInfo fileInfo)
    {
        var validationResult = new ValidationResult()
        {
            IsValid = true,
            ErrorMessage = string.Empty,
        };
        if (fileInfo.Length == 0)
        {
            validationResult.ErrorMessage += "File is empty.\n";
            validationResult.IsValid = false;
        }
        else if (fileInfo.Length > config.MaxFileSizeInBytes)
        {
            validationResult.ErrorMessage +=
                $"File size is too big, max allowed size: {config.MaxFileSizeInBytes}\n";
            validationResult.IsValid = false;
        }
        if (config.AllowedExtensions.Count != 0 && !config.AllowedExtensions.Contains(fileInfo.Extension))
        {
            validationResult.ErrorMessage +=
                "File extension is not allowed.\n";
            validationResult.IsValid = false;
        }
        return validationResult;
    }
}
