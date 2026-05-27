using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Model;
using Spikylin.Service;

namespace Spikylin.Pages.Blog
{
    public class PostModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMarkdownService _markdownParser;

        public PostModel(IWebHostEnvironment env, IMarkdownService markdownParser)
        {
            _env = env;
            _markdownParser = markdownParser;
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
                var markdown = _markdownParser.Parse(markdownContent, filePath);
                Post = new Post
                {
                    FileName = fileName,
                    Markdown = markdown
                };
            }
        }
    }
}
