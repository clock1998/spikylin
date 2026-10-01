namespace Spikylin.Features.Blog.Dto;

public class MarkdownContent
{
    public MarkdownMetadata Meta { get; init; } = new MarkdownMetadata();
    public string Html { get; init; } = string.Empty;
}
