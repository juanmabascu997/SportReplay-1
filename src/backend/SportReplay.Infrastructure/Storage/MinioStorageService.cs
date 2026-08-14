using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Options;

namespace SportReplay.Infrastructure.Storage;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _client;
    private readonly StorageOptions _options;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IOptions<StorageOptions> options, ILogger<MinioStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint.Replace("http://", string.Empty).Replace("https://", string.Empty))
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
    }

    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_options.Bucket), cancellationToken);
            if (!exists)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_options.Bucket), cancellationToken);
                _logger.LogInformation("Created storage bucket {Bucket}", _options.Bucket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure storage bucket {Bucket}", _options.Bucket);
        }
    }

    public async Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_options.Bucket)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType), cancellationToken);
    }

    public async Task UploadFileAsync(string objectKey, string filePath, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_options.Bucket)
            .WithObject(objectKey)
            .WithFileName(filePath)
            .WithContentType(contentType), cancellationToken);
    }

    public async Task<string> GetSignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        return await _client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(_options.Bucket)
            .WithObject(objectKey)
            .WithExpiry((int)expiry.TotalSeconds));
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_options.Bucket).WithObject(objectKey), cancellationToken);
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.StatObjectAsync(new StatObjectArgs().WithBucket(_options.Bucket).WithObject(objectKey), cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
