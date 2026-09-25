using Amazon.S3;
using Amazon.S3.Model;

namespace Spikylin.Service;

public sealed class S3ThumbnailSynchronizationService(
    S3Clients s3Clients,
    IThumbnailService thumbnailService,
    IConfiguration configuration,
    ILogger<S3ThumbnailSynchronizationService> logger)
{
    private static readonly string[] ImageExtensions = [".avif", ".gif", ".jpeg", ".jpg", ".png", ".webp"];
    private readonly S3PhotoOptions options = configuration.GetSection("S3").Get<S3PhotoOptions>() ?? new();

    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        var originals = await ListObjectsAsync(s3Clients.Public, options.PublicBucket.BucketName, options.PublicBucket.Prefix, cancellationToken).ConfigureAwait(false);
        var thumbnails = await ListObjectsAsync(s3Clients.Thumbnails, options.ThumbnailBucket.BucketName, string.Empty, cancellationToken).ConfigureAwait(false);
        var originalKeys = originals
            .Where(item => IsImage(item.Key))
            .Select(item => GetThumbnailKey(item.Key))
            .ToHashSet(StringComparer.Ordinal);
        var thumbnailsByKey = thumbnails.ToDictionary(item => item.Key, StringComparer.Ordinal);

        foreach (var original in originals.Where(item => IsImage(item.Key)))
        {
            var thumbnailKey = GetThumbnailKey(original.Key);
            if (!thumbnailsByKey.TryGetValue(thumbnailKey, out var thumbnail)
                || original.LastModified > thumbnail.LastModified)
            {
                await CreateThumbnailAsync(original.Key, thumbnailKey, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var thumbnail in thumbnails.Where(item => !originalKeys.Contains(item.Key)))
        {
            await s3Clients.Thumbnails.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = options.ThumbnailBucket.BucketName,
                Key = thumbnail.Key,
            }, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Deleted orphaned thumbnail {ThumbnailKey}", thumbnail.Key);
        }
    }

    private async Task CreateThumbnailAsync(string sourceKey, string thumbnailKey, CancellationToken cancellationToken)
    {
        using var sourceResponse = await s3Clients.Public.GetObjectAsync(new GetObjectRequest
        {
            BucketName = options.PublicBucket.BucketName,
            Key = sourceKey,
        }, cancellationToken).ConfigureAwait(false);

        var thumbnail = await thumbnailService.CreateAsync(sourceResponse.ResponseStream, cancellationToken).ConfigureAwait(false);
        await using var stream = new MemoryStream(thumbnail.Content, writable: false);
        try
        {
            var uploadResponse = await s3Clients.Thumbnails.PutObjectAsync(new PutObjectRequest
            {
                BucketName = options.ThumbnailBucket.BucketName,
                Key = thumbnailKey,
                InputStream = stream,
                ContentType = thumbnail.ContentType
            }, cancellationToken).ConfigureAwait(false);
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

    private string GetThumbnailKey(string sourceKey)
    {
        var relativeKey = sourceKey.StartsWith(options.PublicBucket.Prefix, StringComparison.OrdinalIgnoreCase)
            ? sourceKey[options.PublicBucket.Prefix.Length..]
            : sourceKey;
        var extension = Path.GetExtension(relativeKey);

        return string.IsNullOrEmpty(extension)
            ? $"{relativeKey}.webp"
            : $"{relativeKey[..^extension.Length]}.webp";
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

    private static bool IsImage(string key) =>
        ImageExtensions.Contains(Path.GetExtension(key), StringComparer.OrdinalIgnoreCase);

    private sealed record StoredObject(string Key, DateTimeOffset LastModified);
}

public sealed class S3ThumbnailSynchronizationWorker(
    S3ThumbnailSynchronizationService synchronizationService,
    IConfiguration configuration,
    ILogger<S3ThumbnailSynchronizationWorker> logger) : BackgroundService
{
    private readonly TimeSpan interval = TimeSpan.FromSeconds(
        Math.Max(30, configuration.GetSection("S3").Get<S3PhotoOptions>()?.ThumbnailBucket.ThumbnailSyncIntervalSeconds ?? 300));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);
        logger.LogInformation("S3 thumbnail synchronization worker started. Interval: {Interval}", interval);

        do
        {
            try
            {
                await synchronizationService.SynchronizeAsync(stoppingToken).ConfigureAwait(false);
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
}
