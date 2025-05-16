using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spikylin.Infrastructure.Helper;

namespace Spikylin.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _env;
        public class Document
        {
            public string FileName { get; set; } = string.Empty;
            public MarkdownDocument MarkdownDocument { get; set; } = new MarkdownDocument();
        }
        [BindProperty]
        public List<Document> Documents { get; set; } = new List<Document>();
        public List<string> Tags => Documents.SelectMany(n => n.MarkdownDocument.Meta.Tags).Distinct().ToList();
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
                    var markdown = MarkdownDocumentParser.Parse(markdownContent);
                    Documents.Add(new Document { FileName = Path.GetFileNameWithoutExtension(file), MarkdownDocument = markdown});
                }
            }
        }
    }
}
