using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Features.Gallery.Dto;
using Spikylin.Features.Gallery.Services.Interfaces;
using Spikylin.Pages.Gallery.Partials;

namespace Spikylin.Pages.Gallery;

public class IndexModel(
    IThumbnailService thumbnailService,
    IConfiguration configuration,
    ILogger<IndexModel> logger) : PageModel
{
    private readonly S3Options options = configuration.GetSection("S3").Get<S3Options>() ?? new();
    public IReadOnlyList<PhotoThumbnail> Thumbnails { get; private set; } = new List<PhotoThumbnail>();

    public record Photo(string OriginalPhotoUrl, string Metadata);
    public string? LoadError { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var photos = await thumbnailService.GetThumbnailsAsync(cancellationToken);
            Thumbnails = photos.ToArray();
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Unable to load photography from the S3 endpoint.");
            LoadError = "Photography is temporarily unavailable.";
        }
    }

    //public async Task<IActionResult> OnGetFullSizePhotoAsync(
    //    string key,
    //    CancellationToken cancellationToken)
    //{
    //    if (string.IsNullOrWhiteSpace(key))
    //    {
    //        return BadRequest();
    //    }

    //    var thumbnail = await galleryService.GetFullSizePhotoAsync(key, cancellationToken);
    //    return File(thumbnail.Content, thumbnail.ContentType);
    //}

    public async Task<IActionResult> OnGetFullSizePhotoAsync(
        Photo photo,
        CancellationToken cancellationToken)
    {
        if (photo is null)
        {
            return BadRequest();
        }

        //var metadata = await galleryService.GetPhotoMetadataAsync(photo.Key, cancellationToken);
        return Partial("Partials/_ModalImagePartial", new _ModalImagePartialModel
        {
            OriginalPhotoUrl = photo.OriginalPhotoUrl,
            Metadata = photo.Metadata
        });
    }
}