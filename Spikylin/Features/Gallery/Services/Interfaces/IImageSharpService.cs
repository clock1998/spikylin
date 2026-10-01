using Spikylin.Features.Gallery.Dto;

namespace Spikylin.Features.Gallery.Services.Interfaces;

public interface IImageSharpService
{
    Task<ThumbnailResult> CreateThumbnailAsync(
        Stream source,
        CancellationToken cancellationToken = default);
}
