using Amazon.S3;
using Amazon.S3.Model;
using System.Globalization;

namespace Spikylin.Service;

public sealed class S3PhotoCatalog(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3PhotoCatalog> logger)
{
    private static readonly string[] ImageExtensions = [".avif", ".gif", ".jpeg", ".jpg", ".png", ".webp"];
    private static readonly string[] DefaultDateHeaders = ["x-amz-meta-photo-date", "x-amz-meta-date", "x-amz-meta-taken-at"];
    private readonly S3PhotoOptions options = configuration.GetSection("Photography:S3").Get<S3PhotoOptions>() ?? new();

    /// <summary>Loads the public image objects and orders them by their photo date.</summary>
    public async Task<IReadOnlyList<PhotoItem>> GetPhotosAsync(CancellationToken cancellationToken = default)
    {
        var objects = new List<S3Object>();
        string? continuationToken = null;

        do
        {
            var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = options.BucketName,
                ContinuationToken = continuationToken,
                MaxKeys = 1_000,
                Prefix = options.Prefix,
            }, cancellationToken).ConfigureAwait(false);

            objects.AddRange(response.S3Objects.Select(item => new S3Object(item.Key, item.LastModified ?? DateTime.UtcNow)));
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        var photos = new List<PhotoItem>(objects.Count);
        foreach (var item in objects.Where(IsImage))
        {
            var photoDate = await GetPhotoDateAsync(item, cancellationToken).ConfigureAwait(false);
            photos.Add(new PhotoItem(item.Key, BuildObjectUri(item.Key), photoDate));
        }

        return photos
            .OrderByDescending(photo => photo.Date)
            .ThenBy(photo => photo.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<DateTimeOffset> GetPhotoDateAsync(S3Object item, CancellationToken cancellationToken)
    {
        try
        {
            var response = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = options.BucketName,
                Key = item.Key,
            }, cancellationToken).ConfigureAwait(false);

            foreach (var headerName in options.DateHeaders ?? DefaultDateHeaders)
            {
                var metadataKey = headerName.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase)
                    ? headerName["x-amz-meta-".Length..]
                    : headerName;

                var metadataKeyInResponse = response.Metadata.Keys.FirstOrDefault(key =>
                    string.Equals(key, headerName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, metadataKey, StringComparison.OrdinalIgnoreCase));

                if (metadataKeyInResponse is not null
                    && TryParseDate(response.Metadata[metadataKeyInResponse], out var date))
                {
                    return date;
                }
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Could not read metadata for photography object {ObjectKey}", item.Key);
        }

        return item.LastModified;
    }

    private static bool TryParseDate(string value, out DateTimeOffset date)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date);
    }

    private Uri BuildObjectUri(string key)
    {
        var endpoint = options.Endpoint.TrimEnd('/');
        var escapedKey = string.Join('/', key.Split('/').Select(Uri.EscapeDataString));
        return new Uri($"{endpoint}/{options.BucketName}/{escapedKey}", UriKind.Absolute);
    }

    private static bool IsImage(S3Object item) => ImageExtensions.Contains(Path.GetExtension(item.Key), StringComparer.OrdinalIgnoreCase);

    private sealed record S3Object(string Key, DateTimeOffset LastModified);
}

public sealed class S3PhotoOptions
{
    public string Endpoint { get; set; } = "https://s3.spikylin.com/public";
    public string BucketName { get; set; } = "public";
    public string Prefix { get; set; } = "photography/";
    public string[] DateHeaders { get; set; } = ["x-amz-meta-photo-date", "x-amz-meta-date", "x-amz-meta-taken-at"];
}

public sealed record PhotoItem(string Key, Uri Url, DateTimeOffset Date);
