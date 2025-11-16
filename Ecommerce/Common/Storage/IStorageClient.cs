namespace Ecommerce.Common.Storage;

public interface IStorageClient
{
    
    Task PutObjectAsync(string bucket, string key, Stream stream);
    
    Task RemoveObjectAsync(string bucket, string key);
    
}