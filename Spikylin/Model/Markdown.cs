namespace Spikylin.Model;

public class Markdown
{
    public MarkdownMetadata Meta { get; init; } = new MarkdownMetadata();
    public string Html { get; init; } = string.Empty;
}
