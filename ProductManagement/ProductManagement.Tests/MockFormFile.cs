using Microsoft.AspNetCore.Http;

namespace ProductManagement.Tests;

public class MockFormFile : IFormFile
{
    private readonly Stream _stream;

    public MockFormFile(Stream stream, string fileName, string contentType, string contentDisposition)
    {
        _stream = stream;
        FileName = fileName;
        ContentType = contentType;
        ContentDisposition = contentDisposition;
        Length = stream.Length;
        Name = "file";
        Headers = new HeaderDictionary();
    }

    public string ContentType { get; }

    public string ContentDisposition { get; set; }

    public IHeaderDictionary Headers { get; }

    public long Length { get; }

    public string Name { get; }

    public string FileName { get; }

    public void CopyTo(Stream target)
    {
        _stream.Seek(0, SeekOrigin.Begin);
        _stream.CopyTo(target);
    }

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        _stream.Seek(0, SeekOrigin.Begin);
        return _stream.CopyToAsync(target, cancellationToken);
    }

    public Stream OpenReadStream()
    {
        _stream.Seek(0, SeekOrigin.Begin);
        return _stream;
    }
}