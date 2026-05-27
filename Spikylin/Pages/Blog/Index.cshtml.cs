using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Spikylin.Model;
using Spikylin.Service;
using System.Globalization;

namespace Spikylin.Pages.Blog
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IStringLocalizer<IndexModel> _localizer;
        private readonly IMarkdownService _markdownParser;
        public List<Post> Posts { get; set; } = new List<Post>();
        public List<string> Tags => Posts.SelectMany(n => n.Markdown.Meta.Tags).Distinct().ToList();
        public IndexModel(
            ILogger<IndexModel> logger, 
            IWebHostEnvironment env, 
            IStringLocalizer<IndexModel> localizer, 
            IMarkdownService markdownParser)
        {
            _logger = logger;
            _env = env;
            _localizer = localizer;
            _markdownParser = markdownParser;
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
            var cultureShortName = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var contentPath = Path.Combine(_env.ContentRootPath, "Posts");
            if (Directory.Exists(contentPath))
            {
                var files = Directory.GetFiles(contentPath, "*.md", SearchOption.AllDirectories)
                    .Where(file => cultureShortName.Equals("fr", StringComparison.OrdinalIgnoreCase)
                        ? file.EndsWith(".fr.md", StringComparison.OrdinalIgnoreCase)
                        : !file.EndsWith(".fr.md", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    var markdownContent = System.IO.File.ReadAllText(file);
                    var markdown = _markdownParser.Parse(markdownContent, file);
                    Posts.Add(new Post { FileName = Path.GetFileNameWithoutExtension(file), Markdown = markdown });
                }
                Posts = Posts.Where(n => n.Markdown.Meta.Published).OrderByDescending(n => n.Markdown.Meta.Date).ToList();
            }
        }
    }
}
