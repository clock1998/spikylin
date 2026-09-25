using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

namespace Spikylin.Service;

public interface IThumbnailService
{
    Task<ThumbnailResult> GetAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<ThumbnailResult> CreateAsync(
        Stream source,
        CancellationToken cancellationToken = default);
}

public sealed record ThumbnailResult(byte[] Content, string ContentType);

public sealed class ImageSharpThumbnailService(
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
        using var response = await s3Clients.Thumbnails.GetObjectAsync(new GetObjectRequest
        {
            BucketName = options.ThumbnailBucket.BucketName,
            Key = GetThumbnailKey(key),
        }, cancellationToken).ConfigureAwait(false);

        await using var output = new MemoryStream();
        await response.ResponseStream.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        return new ThumbnailResult(output.ToArray(), response.Headers.ContentType ?? "image/jpeg");
    }

    public async Task<ThumbnailResult> CreateAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync(source, cancellationToken).ConfigureAwait(false);
        image.Mutate(context => context.Resize(new ResizeOptions
        {
            Size = new Size(ThumbnailSize, ThumbnailSize),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center,
        }));

        await using var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 82 }, cancellationToken).ConfigureAwait(false);

        return new ThumbnailResult(output.ToArray(), "image/webp");
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
}