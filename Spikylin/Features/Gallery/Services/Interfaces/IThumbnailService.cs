using Spikylin.Features.Gallery.Dto;

namespace Spikylin.Features.Gallery.Services.Interfaces;

public interface IThumbnailService
{
    public Task<IReadOnlyList<PhotoThumbnail>> GetThumbnailsAsync(CancellationToken cancellationToken = default);
}
