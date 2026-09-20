using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Spikylin.Service;

public interface IThumbnailService
{
    Task<ThumbnailResult> CreateAsync(
        string key,
        CancellationToken cancellationToken = default);
}

public sealed record ThumbnailResult(byte[] Content, string ContentType);

public sealed class ImageSharpThumbnailService(
    IAmazonS3 s3Client,
    IConfiguration configuration) : IThumbnailService
{
    private const int ThumbnailSize = 600;
    private readonly S3PhotoOptions options =
        configuration.GetSection("S3").Get<S3PhotoOptions>() ?? new();

    public async Task<ThumbnailResult> CreateAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        using var response = await s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = options.BucketName,
            Key = key,
        }, cancellationToken).ConfigureAwait(false);

        using var image = await Image.LoadAsync(response.ResponseStream, cancellationToken).ConfigureAwait(false);
        image.Mutate(context => context.Resize(new ResizeOptions
        {
            Size = new Size(ThumbnailSize, ThumbnailSize),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center,
        }));

        await using var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 82 }, cancellationToken).ConfigureAwait(false);

        return new ThumbnailResult(output.ToArray(), "image/jpeg");
    }
}