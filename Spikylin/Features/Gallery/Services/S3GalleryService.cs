using Amazon.S3.Model;
using Spikylin.Features.Shared;

namespace Spikylin.Features.Gallery.Services;

public sealed class S3GalleryService(
    S3Clients s3Clients, 
    IConfiguration configuration, 
    ILogger<S3GalleryService> logger)
{
    private readonly S3Options options = configuration.GetSection("S3").Get<S3Options>() ?? new();
    public record ThumbnailResult(byte[] Content, string ContentType);
    public async Task<ThumbnailResult> GetFullSizePhotoAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        using var response = await s3Clients.SpikylinS3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = options.SpikylinS3Bucket.BucketName,
            Key = key,
        }, cancellationToken).ConfigureAwait(false);

        await using var output = new MemoryStream();
        await response.ResponseStream.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        return new ThumbnailResult(output.ToArray(), response.Headers.ContentType ?? "image/jpeg");
    }

}
