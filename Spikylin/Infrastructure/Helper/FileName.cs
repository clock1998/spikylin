using Markdig;
namespace Spikylin.Infrastructure.Helper
{
    public static class MarkdownHelper
    {
        public static string ToHtml(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            return Markdig.Markdown.ToHtml(markdown, pipeline);
        }
    }

}
