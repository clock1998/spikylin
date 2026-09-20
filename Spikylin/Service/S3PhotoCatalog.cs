using Amazon.S3;
using Amazon.S3.Model;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace Spikylin.Service;

public sealed class S3PhotoCatalog(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3PhotoCatalog> logger)
{
    private static readonly string[] ImageExtensions = [".avif", ".gif", ".jpeg", ".jpg", ".png", ".webp"];
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
            var metadata = await GetPhotoMetadataAsync(item, cancellationToken).ConfigureAwait(false);
            photos.Add(new PhotoItem(item.Key, BuildObjectUri(item.Key), metadata.Date, metadata.Details));
        }

        return photos
            .OrderByDescending(photo => photo.Date)
            .ThenBy(photo => photo.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<PhotoMetadataResult> GetPhotoMetadataAsync(S3Object item, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await s3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = options.BucketName,
                Key = item.Key,
            }, cancellationToken).ConfigureAwait(false);

            using var seekableStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(seekableStream, cancellationToken).ConfigureAwait(false);
            seekableStream.Position = 0;

            var directories = ImageMetadataReader.ReadMetadata(seekableStream);

            return new PhotoMetadataResult(
                item.LastModified,
                new PhotoMetadata(
                    GetExifValue<ExifIfd0Directory>(directories, ExifDirectoryBase.TagModel),
                    GetExifValue<ExifSubIfdDirectory>(directories, ExifDirectoryBase.TagDateTimeOriginal),
                    GetExifValue(directories, ExifDirectoryBase.TagFocalLength),
                    GetExifValue(directories, ExifDirectoryBase.TagFNumber),
                    GetExifValue(directories, ExifDirectoryBase.TagIsoEquivalent),
                    GetExifValue(directories, ExifDirectoryBase.TagExposureTime)));
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Could not read metadata for photography object {ObjectKey}", item.Key);
        }
        catch (ImageProcessingException exception)
        {
            logger.LogWarning(exception, "Could not read EXIF data for photography object {ObjectKey}", item.Key);
        }
        catch (AmazonS3Exception exception)
        {
            logger.LogWarning(exception, "Could not download photography object {ObjectKey} for EXIF data", item.Key);
        }

        return new PhotoMetadataResult(item.LastModified, new PhotoMetadata(null, null, null, null, null, null));
    }

    private static string? GetExifValue(IReadOnlyList<MetadataExtractor.Directory> directories, int tag) =>
        GetExifValue<ExifSubIfdDirectory>(directories, tag);

    private static string? GetExifValue<TDirectory>(IReadOnlyList<MetadataExtractor.Directory> directories, int tag)
        where TDirectory : MetadataExtractor.Directory
    {
        return directories.OfType<TDirectory>()
            .Select(directory => directory.GetDescription(tag))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private Uri BuildObjectUri(string key)
    {
        var endpoint = options.Endpoint.TrimEnd('/');
        var escapedKey = string.Join('/', key.Split('/').Select(Uri.EscapeDataString));
        return new Uri($"{endpoint}/{options.BucketName}/{escapedKey}", UriKind.Absolute);
    }

    private static bool IsImage(S3Object item) => ImageExtensions.Contains(Path.GetExtension(item.Key), StringComparer.OrdinalIgnoreCase);

    private sealed record S3Object(string Key, DateTimeOffset LastModified);
    private sealed record PhotoMetadataResult(DateTimeOffset Date, PhotoMetadata Details);
}

public sealed class S3PhotoOptions
{
    public string Endpoint { get; set; } = "https://s3.spikylin.com/public";
    public string BucketName { get; set; } = "public";
    public string Prefix { get; set; } = "photography/";
}

public sealed record PhotoMetadata(
    string? CameraModel,
    string? DateTime,
    string? FocalLength,
    string? Aperture,
    string? Iso,
    string? ShutterSpeed)
{
    public string DisplayText => string.Join(" · ",
        new[]
        {
            CameraModel,
            DateTime,
            FocalLength,
            Aperture,
            Iso is null ? null : $"ISO {Iso}",
            ShutterSpeed,
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed record PhotoItem(string Key, Uri Url, DateTimeOffset Date, PhotoMetadata Metadata);

