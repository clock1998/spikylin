using Spikylin.Features.Blog.Dto;

namespace Spikylin.Features.Blog;

public interface IMarkdownService
{
    MarkdownContent Parse(string markdown, string? filePath = null);
}
