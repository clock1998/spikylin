
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Spikylin.Pages.Gallery.Partials
{
    public class _ModalImagePartialModel : PageModel
    {
        public string Metadata { get; internal set; }
        public string OriginalPhotoUrl { get; internal set; }

        public void OnGet()
        {
        }
    }
}
