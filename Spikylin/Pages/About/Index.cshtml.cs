using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Infrastructure.Helper;
using Spikylin.Infrastructure.Model;

namespace Spikylin.Pages.About;

public class AboutModel(IWebHostEnvironment env) : PageModel
{
    public string Content { get; set; } = string.Empty;
    public void OnGet()
    {
        // Use the fileName to locate and load the document.
        var contentPath = Path.Combine(env.ContentRootPath, "Pages/About");
        var filePath = Path.Combine(contentPath,  "content.md");
        if (System.IO.File.Exists(filePath))
        {
            var markdownContent = System.IO.File.ReadAllText(filePath);
            Content = MarkdigMarkdownParser.Parse(markdownContent).Html;
        }
    }
}