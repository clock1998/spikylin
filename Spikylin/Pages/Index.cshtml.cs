using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Spikylin.Infrastructure.Helper;
using Spikylin.Infrastructure.Model;

namespace Spikylin.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IStringLocalizer<IndexModel> _localizer;
        public List<Post> Posts { get; set; } = new List<Post>();
        public List<string> Tags => Posts.SelectMany(n => n.Markdown.Meta.Tags).Distinct().ToList();
        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment env, IStringLocalizer<IndexModel> localizer)
        {
            _logger = logger;
            _env = env;
            _localizer = localizer;
        }
        public void OnGet()
        {
            LoadPosts();
        }

        public IActionResult OnGetFilterPosts(string tag)
        {
            LoadPosts();
            var filteredPosts = Posts.Where(n => n.Markdown.Meta.Tags.Contains(tag)).ToList();
            return Partial("_PostsPartial", filteredPosts);
        }

        private void LoadPosts()
        {
            Posts.Clear();
            var contentPath = Path.Combine(_env.ContentRootPath, "Posts");
            if (Directory.Exists(contentPath))
            {
                var files = Directory.GetFiles(contentPath, "*.md", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var markdownContent = System.IO.File.ReadAllText(file);
                    var markdown = MarkdigMarkdownParser.Parse(markdownContent);
                    Posts.Add(new Post { FileName = Path.GetFileNameWithoutExtension(file), Markdown = markdown });
                }
            }
        }
    }
}
