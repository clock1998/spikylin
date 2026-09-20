using Spikylin.Model;

namespace Spikylin.Service;

public interface IMarkdownService
{
    Markdown Parse(string markdown, string? filePath = null);
}
