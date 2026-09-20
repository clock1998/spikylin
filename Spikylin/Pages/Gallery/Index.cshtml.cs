using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Service;

namespace Spikylin.Pages.Gallery;

public class IndexModel(S3PhotoCatalog photoCatalog, ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<PhotoItem> Photos { get; private set; } = [];

    public string? LoadError { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Photos = await photoCatalog.GetPhotosAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Unable to load photography from the S3 endpoint.");
            LoadError = "Photography is temporarily unavailable.";
        }

    }
}