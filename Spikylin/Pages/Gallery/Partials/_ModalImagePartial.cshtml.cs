using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Service;

namespace Spikylin.Pages.Gallery.Partials
{
    public class _ModalImagePartialModel : PageModel
    {
        public PhotoItem Photo { get; internal set; }
        public PhotoMetadata Metadata { get; internal set; }

        public void OnGet()
        {
        }
    }
}
