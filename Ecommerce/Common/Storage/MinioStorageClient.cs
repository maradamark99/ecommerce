using Minio;
using Minio.DataModel.Args;

namespace Ecommerce.Common.Storage;

public class MinioStorageClient(IMinioClient client) : IStorageClient
{
    public async Task PutObjectAsync(string bucket, string key, Stream stream)
    {
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(key)
            .WithObjectSize(stream.Length)
            .WithStreamData(stream);
        await client.PutObjectAsync(putObjectArgs);
    }

    public async Task RemoveObjectAsync(string bucket, string key)
    {
        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(bucket)
            .WithObject(key);
        await client.RemoveObjectAsync(removeObjectArgs);
    }
}