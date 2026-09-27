using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using Spikylin.Core;

namespace Spikylin.Service.Worker
{
    public sealed class S3ThumbnailSynchronizationWorker(
    S3Clients s3Clients,
    IImageSharpService imageSharpService,
    IConfiguration configuration,
    ILogger<S3ThumbnailSynchronizationWorker> logger) : BackgroundService
    {
        private readonly S3Options options = configuration.GetSection("S3").Get<S3Options>() ?? new();

        private readonly TimeSpan interval = TimeSpan.FromSeconds(
            Math.Max(30, 300));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(interval);
            logger.LogInformation("S3 thumbnail synchronization worker started. Interval: {Interval}", interval);

            do
            {
                try
                {
                    await SynchronizeAsync(stoppingToken).ConfigureAwait(false);
                    logger.LogInformation("S3 thumbnail synchronization completed.");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "S3 thumbnail synchronization failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }

        private async Task SynchronizeAsync(CancellationToken cancellationToken)
        {
            var originals = await ListObjectsAsync(s3Clients.SpikylinS3, options.SpikylinS3Bucket.BucketName, "gallery/", cancellationToken).ConfigureAwait(false);
            var thumbnails = await ListObjectsAsync(s3Clients.SpikylinS3, options.SpikylinS3Bucket.BucketName, "gallery-thumbnail/", cancellationToken).ConfigureAwait(false);
            var originalKeys = originals
                .Where(item => Helper.IsImage(item.Key))
                .Select(item => BuildThumbnailKey(options.SpikylinS3Bucket.Prefix, item.Key))
                .ToHashSet(StringComparer.Ordinal);
            var thumbnailsByKey = thumbnails.Where(item => Helper.IsImage(item.Key)).ToDictionary(item => item.Key, StringComparer.Ordinal);

            foreach (var original in originals.Where(item => Helper.IsImage(item.Key)))
            {
                var thumbnailKey = BuildThumbnailKey(options.SpikylinS3Bucket.Prefix, original.Key);
                if (!thumbnailsByKey.TryGetValue(thumbnailKey, out var thumbnail)
                    || original.LastModified > thumbnail.LastModified)
                {
                    await UploadThumbnailAsync(original.Key, thumbnailKey, cancellationToken).ConfigureAwait(false);
                }
            }

            foreach (var thumbnail in thumbnails.Where(item => !originalKeys.Contains(item.Key)))
            {
                await s3Clients.SpikylinS3.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = options.SpikylinS3Bucket.BucketName,
                    Key = thumbnail.Key,
                }, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Deleted orphaned thumbnail {ThumbnailKey}", thumbnail.Key);
            }
        }

        private async Task UploadThumbnailAsync(string sourceKey, string thumbnailKey, CancellationToken cancellationToken)
        {
            using var sourceResponse = await s3Clients.SpikylinS3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = options.SpikylinS3Bucket.BucketName,
                Key = sourceKey,
            }, cancellationToken).ConfigureAwait(false);
            var thumbnail = await imageSharpService.CreateThumbnailAsync(sourceResponse.ResponseStream, cancellationToken).ConfigureAwait(false);
            
            await using var stream = new MemoryStream(thumbnail.Content, writable: false);
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = options.SpikylinS3Bucket.BucketName,
                    Key = thumbnailKey,
                    InputStream = stream,
                    ContentType = thumbnail.ContentType,
                    UseChunkEncoding = false,
                };
                request.Metadata.Add("x-amz-meta-camera-model", thumbnail.PhotoMetadata.CameraModel ?? string.Empty);
                request.Metadata.Add("x-amz-meta-date-taken", thumbnail.PhotoMetadata.DateTime ?? string.Empty);
                request.Metadata.Add("x-amz-meta-focal-length", thumbnail.PhotoMetadata.FocalLength ?? string.Empty);
                request.Metadata.Add("x-amz-meta-f-number", thumbnail.PhotoMetadata.Aperture ?? string.Empty);
                request.Metadata.Add("x-amz-meta-iso", thumbnail.PhotoMetadata.Iso ?? string.Empty);
                request.Metadata.Add("x-amz-meta-exposure-time", thumbnail.PhotoMetadata.ShutterSpeed ?? string.Empty);
                request.Metadata.Add("x-amz-meta-exif", thumbnail.PhotoMetadata.DisplayText ?? string.Empty);
                request.Metadata.Add("x-amz-meta-original-photo-key", sourceKey ?? string.Empty);
                var uploadResponse = await s3Clients.SpikylinS3.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonS3Exception exception)
            {
                logger.LogError(
                    exception,
                    "Thumbnail upload failed. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, RequestId: {RequestId}, HostId: {HostId}",
                    exception.StatusCode,
                    exception.ErrorCode,
                    exception.RequestId,
                    exception.ResponseBody);

                throw;
            }

        }

        private async Task<IReadOnlyList<StoredObject>> ListObjectsAsync(
            IAmazonS3 client,
            string bucketName,
            string prefix,
            CancellationToken cancellationToken)
        {
            var objects = new List<StoredObject>();
            string? continuationToken = null;

            do
            {
                var response = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    ContinuationToken = continuationToken,
                    MaxKeys = 1_000,
                    Prefix = prefix,
                }, cancellationToken).ConfigureAwait(false);

                objects.AddRange((response.S3Objects ?? []).Select(item => new StoredObject(
                    item.Key,
                    item.LastModified ?? DateTime.UtcNow)));
                continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
            }
            while (!string.IsNullOrWhiteSpace(continuationToken));

            return objects;
        }

        private string BuildThumbnailKey(string prefix, string sourceKey)
        {
            var relativeKey = sourceKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? sourceKey[prefix.Length..]
                : sourceKey;
            relativeKey = relativeKey.Replace("gallery", "gallery-thumbnail");
            var extension = Path.GetExtension(sourceKey);

            return string.IsNullOrEmpty(extension)
                ? $"{relativeKey}.webp"
                : $"{relativeKey[..^extension.Length]}.webp";
        }
        private sealed record StoredObject(string Key, DateTimeOffset LastModified);
    }
}
