
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Spikylin.Pages.Gallery.Partials
{
    public class _ModalImagePartialModel : PageModel
    {
        public Uri Uri { get; internal set; }
        public string Metadata { get; internal set; }

        public void OnGet()
        {
        }
    }
}
