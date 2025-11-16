using EcommerceLib.Storage;

namespace ProductManagement.Tests;

public class MockStorageClient : IStorageClient
{
    public Task PutObjectAsync(string bucket, string key, Stream stream)
    {
        return Task.CompletedTask;
    }

    public Task RemoveObjectAsync(string bucket, string key)
    {
        return Task.CompletedTask;
    }
}