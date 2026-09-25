using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

namespace Spikylin.Service;

public interface IImageSharpService
{
    Task<ThumbnailResult> CreateThumbnailAsync(
        Stream source,
        CancellationToken cancellationToken = default);
}

public sealed class ImageSharpService() : IImageSharpService
{
    private const int ThumbnailSize = 600;

    public async Task<ThumbnailResult> CreateThumbnailAsync(
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
}