using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Infrastructure.Helper;
using Spikylin.Infrastructure.Model;

namespace Spikylin.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _env;

        [BindProperty]
        public List<Post> Documents { get; set; } = new List<Post>();
        public List<string> Tags => Documents.SelectMany(n => n.Markdown.Meta.Tags).Distinct().ToList();
        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }
        public void OnGet()
        {
            var contentPath = Path.Combine(_env.ContentRootPath, "Posts");
            
            if (Directory.Exists(contentPath))
            {
                var files = Directory.GetFiles(contentPath, "*.md", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var markdownContent = System.IO.File.ReadAllText(file);
                    var markdown = MarkdigMarkdownParser.Parse(markdownContent);
                    Documents.Add(new Post { FileName = Path.GetFileNameWithoutExtension(file), Markdown = markdown});
                }
            }
        }
    }
}
