using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using Spikylin.Core;
using Spikylin.Core.Model;
using System.Text.Json;

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
            List<PhotoThumbnail> newOrUpdatedThumbnails = new List<PhotoThumbnail>();
            foreach (var original in originals.Where(item => Helper.IsImage(item.Key)))
            {
                var thumbnailKey = BuildThumbnailKey(options.SpikylinS3Bucket.Prefix, original.Key);
                if (!thumbnailsByKey.TryGetValue(thumbnailKey, out var thumbnail)
                    || original.LastModified > thumbnail.LastModified)
                {
                    newOrUpdatedThumbnails.Add(await UploadThumbnailAsync(original.Key, thumbnailKey, cancellationToken).ConfigureAwait(false));
                }
            }
            var orphanedThumbnails = thumbnails.Where(item => !originalKeys.Contains(item.Key));
            foreach (var thumbnail in orphanedThumbnails)
            {
                await s3Clients.SpikylinS3.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = options.SpikylinS3Bucket.BucketName,
                    Key = thumbnail.Key,
                }, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Deleted orphaned thumbnail {ThumbnailKey}", thumbnail.Key);
            }
            
            using var existingManifest = await s3Clients.SpikylinS3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = options.SpikylinS3Bucket.BucketName,
                Key = "manifest.json"
            }, cancellationToken).ConfigureAwait(false);

            var existingThumbnails = await JsonSerializer.DeserializeAsync<List<PhotoThumbnail>>(
                existingManifest.ResponseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if(existingThumbnails is not null)
            {
                existingThumbnails.RemoveAll(thumbnail => orphanedThumbnails.Any(orphan => orphan.Key == thumbnail.Key));
                existingThumbnails.AddRange(newOrUpdatedThumbnails);
                var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(existingThumbnails, jsonOptions);
                await using var stream = new MemoryStream(jsonBytes);
                await s3Clients.SpikylinS3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = options.SpikylinS3Bucket.BucketName,
                    Key = "manifest.json",
                    InputStream = stream,
                    ContentType = "application/json",
                    UseChunkEncoding = false
                }, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(newOrUpdatedThumbnails, jsonOptions);
                await using var stream = new MemoryStream(jsonBytes);
                await s3Clients.SpikylinS3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = options.SpikylinS3Bucket.BucketName,
                    Key = "manifest.json",
                    InputStream = stream,
                    ContentType = "application/json",
                    UseChunkEncoding = false
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<PhotoThumbnail> UploadThumbnailAsync(string originalPhotoKey, string thumbnailKey, CancellationToken cancellationToken)
        {
            using var sourceResponse = await s3Clients.SpikylinS3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = options.SpikylinS3Bucket.BucketName,
                Key = originalPhotoKey,
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
                var metadata = new PhotoMetadata(
                    thumbnail.PhotoMetadata.CameraModel ?? string.Empty,
                    thumbnail.PhotoMetadata.DateTime ?? string.Empty,
                    thumbnail.PhotoMetadata.FocalLength ?? string.Empty,
                    thumbnail.PhotoMetadata.Aperture ?? string.Empty,
                    thumbnail.PhotoMetadata.Iso ?? string.Empty,
                    thumbnail.PhotoMetadata.ShutterSpeed ?? string.Empty,
                    Helper.BuildUri(options.WebsiteEndpoint, originalPhotoKey).AbsoluteUri
                );                
                var uploadResponse = await s3Clients.SpikylinS3.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
                return new PhotoThumbnail (thumbnailKey, Helper.BuildUri(options.WebsiteEndpoint, thumbnailKey), metadata);
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
