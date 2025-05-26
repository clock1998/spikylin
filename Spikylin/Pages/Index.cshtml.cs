using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Infrastructure.Helper;
using System.Globalization;

namespace Spikylin.Pages
{
    public class IndexModel(IWebHostEnvironment env) : PageModel
    {
        public string Html { get; set; } = string.Empty;
        public void OnGet()
        {
            // Use the fileName to locate and load the document.
            var contentPath = Path.Combine(env.ContentRootPath, "Pages");
            var filePath = CultureInfo.CurrentCulture.Name == "en" ? Path.Combine(contentPath, "content.md") : Path.Combine(contentPath, "content.fr.md");
            if (System.IO.File.Exists(filePath))
            {
                var markdownContent = System.IO.File.ReadAllText(filePath);
                Html = MarkdigMarkdownParser.Parse(markdownContent).Html;
            }
        }
    }
}
