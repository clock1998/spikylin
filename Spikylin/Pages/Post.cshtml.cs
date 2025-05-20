using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Infrastructure.Helper;
using Spikylin.Model;

namespace Spikylin.Pages
{
    public class PostModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        public PostModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        [BindProperty(SupportsGet = true)]
        public required Post Post { get; set; }
        public void OnGet(string fileName)
        {
            // Use the fileName to locate and load the document.
            var contentPath = Path.Combine(_env.ContentRootPath, "Posts");
            var filePath = Path.Combine(contentPath, fileName + ".md");
            if (System.IO.File.Exists(filePath))
            {
                var markdownContent = System.IO.File.ReadAllText(filePath);
                var markdown = MarkdigMarkdownParser.Parse(markdownContent);
                Post = new Post
                {
                    FileName = fileName,
                    Markdown = markdown
                };
            }
        }
    }
}
