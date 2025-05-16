using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Spikylin.Pages
{
    public class PostModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public required string FileName { get; set; }
        public void OnGet()
        {
        }
    }
}
