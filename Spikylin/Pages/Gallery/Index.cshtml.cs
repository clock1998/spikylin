using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Pages.Gallery.Partials;
using Spikylin.Service;

namespace Spikylin.Pages.Gallery;

public class IndexModel(
    S3GalleryService galleryService,
    IThumbnailService thumbnailService,
    ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<PhotoItem> Photos { get; private set; } = new List<PhotoItem>();

    public string? LoadError { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var photos = await galleryService.GetPhotosAsync(cancellationToken);
            Photos = photos
                .Select(photo => photo with
                {
                    ThumbnailUrl = Url.Page("/Gallery/Index", "Thumbnail", new { key = photo.Key }),
                })
                .ToArray();
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Unable to load photography from the S3 endpoint.");
            LoadError = "Photography is temporarily unavailable.";
        }
    }

    public async Task<IActionResult> OnGetThumbnailAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest();
        }

        var thumbnail = await thumbnailService.GetAsync(key, cancellationToken);
        return File(thumbnail.Content, thumbnail.ContentType);
    }

    public async Task<IActionResult> OnGetPhotoMetadataAsync(
        PhotoItem photo,
        CancellationToken cancellationToken)
    {
        if (photo is null)
        {
            return BadRequest();
        }

        var metadata = await galleryService.GetPhotoMetadataAsync(photo.Key, cancellationToken);
        return Partial("Partials/_ModalImagePartial", new _ModalImagePartialModel
        {
            Photo = photo,
            Metadata = metadata
        });
    }
}