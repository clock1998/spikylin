
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Spikylin.Pages.Gallery.Partials
{
    public class _ModalImagePartialModel : PageModel
    {
        public required string Metadata { get; set; }
        public required string OriginalPhotoUrl { get; set; }

        public void OnGet()
        {
        }
    }
}
