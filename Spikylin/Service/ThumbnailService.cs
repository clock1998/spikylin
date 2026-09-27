using Amazon.S3.Model;
using Spikylin.Core;
using Spikylin.Core.Model;

namespace Spikylin.Service;

public interface IThumbnailService
{
    public Task<IReadOnlyList<PhotoThumbnail>> GetThumbnailsAsync(CancellationToken cancellationToken = default);
}
public record PhotoThumbnail(string Key, Uri Url, PhotoMetadata PhotoMetadata);

public class ThumbnailService(
    S3Clients s3Clients,
    IConfiguration configuration) : IThumbnailService
{
    private readonly S3Options options = configuration.GetSection("S3").Get<S3Options>() ?? new();
    public async Task<IReadOnlyList<PhotoThumbnail>> GetThumbnailsAsync(CancellationToken cancellationToken = default)
    {
        var s3objects = new List<S3Object>();
        string? continuationToken = null;

        do
        {
            var response = await s3Clients.SpikylinS3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = options.SpikylinS3Bucket.BucketName,
                ContinuationToken = continuationToken,
                MaxKeys = 1_000,
                Prefix = "gallery-thumbnail/",
            }, cancellationToken).ConfigureAwait(false);
            s3objects.AddRange((response.S3Objects ?? []).Select(item => new S3Object(item.Key, item.LastModified ?? DateTime.UtcNow)));
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        var thumbnails = new List<PhotoThumbnail>(s3objects.Count);
        foreach (var item in s3objects.Where(item => Helper.IsImage(item.Key)))
        {
            var metadata = await s3Clients.SpikylinS3.GetObjectMetadataAsync(
            new GetObjectMetadataRequest
            {
                BucketName = options.SpikylinS3Bucket.BucketName,
                Key = item.Key
            },
            cancellationToken);
            var photoMetadata = new PhotoMetadata(
                metadata.Metadata["x-amz-meta-camera-model"],
                metadata.Metadata["x-amz-meta-date-taken"],
                metadata.Metadata["x-amz-meta-focal-length"],
                metadata.Metadata["x-amz-meta-f-number"],
                metadata.Metadata["x-amz-meta-iso"],
                metadata.Metadata["x-amz-meta-exposure-time"],
                metadata.Metadata["x-amz-meta-original-photo-key"]);
            thumbnails.Add(new PhotoThumbnail(item.Key, Helper.BuildUri(options.WebsiteEndpoint, item.Key), photoMetadata));
        }

        return thumbnails
            .OrderByDescending(thumbnail => thumbnail.PhotoMetadata.DateTime)
            .ThenBy(thumbnail => thumbnail.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record S3Object(string Key, DateTimeOffset LastModified);

    private const int ThumbnailSize = 600;

}