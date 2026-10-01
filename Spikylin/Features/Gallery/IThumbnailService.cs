using Spikylin.Features.Gallery.Dto;

namespace Spikylin.Features.Gallery;

public interface IThumbnailService
{
    public Task<IReadOnlyList<PhotoThumbnail>> GetThumbnailsAsync(CancellationToken cancellationToken = default);
}
