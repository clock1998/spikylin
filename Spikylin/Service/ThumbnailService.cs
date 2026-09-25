using Amazon.S3;
using Amazon.S3.Model;
using Spikylin.Core;

namespace Spikylin.Service;

public interface IThumbnailService
{
    Task<ThumbnailResult> GetAsync(
        string key,
        CancellationToken cancellationToken = default);
}

public sealed record ThumbnailResult(byte[] Content, string ContentType);

public sealed class ThumbnailService(
    S3Clients s3Clients,
    IConfiguration configuration) : IThumbnailService
{
    private const int ThumbnailSize = 600;
    private readonly S3PhotoOptions options =
        configuration.GetSection("S3").Get<S3PhotoOptions>() ?? new();

    public async Task<ThumbnailResult> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        using var response = await s3Clients.SpikylinS3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = options.SpikylinS3Bucket.BucketName,            
            Key = Helper.BuildThumbnailKey(options.SpikylinS3Bucket.Prefix, key),
        }, cancellationToken).ConfigureAwait(false);

        await using var output = new MemoryStream();
        await response.ResponseStream.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        return new ThumbnailResult(output.ToArray(), response.Headers.ContentType ?? "image/jpeg");
    }
}