namespace Spikylin.Features.Blog.Dto
{
    public class BlogPost
    {
        public string FileName { get; set; } = string.Empty;
        public MarkdownContent Markdown { get; set; } = new MarkdownContent();
    }
}
