using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using System.Net;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Spikylin.Service;

public class MarkdigMarkdownService : IMarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UseAdvancedExtensions()
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>
    /// Parses markdown with optional YAML front matter and renders HTML output.
    /// </summary>
    /// <param name="markdown">The markdown source to parse.</param>
    /// <param name="filePath">The source file path used for diagnostics.</param>
    /// <returns>A parsed markdown result with metadata and rendered HTML.</returns>
    public Markdown Parse(string markdown, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var document = Markdig.Markdown.Parse(markdown, Pipeline);

        var yamlBlock = document
            .Descendants<YamlFrontMatterBlock>()
            .FirstOrDefault();

        var meta = new MarkdownMetadata();
        if (yamlBlock != null)
        {
            var lines = yamlBlock.Lines.Lines
                .Select(l => l.Slice.ToString());
            var yaml = string.Join("\n", lines);

            try
            {
                meta = Deserializer.Deserialize<MarkdownMetadata>(yaml) ?? new MarkdownMetadata();
            }
            catch (YamlDotNet.Core.SemanticErrorException)
            {
                meta = new MarkdownMetadata();
            }
        }

        meta.Tags ??= new List<string>();

        var html = Markdig.Markdown.ToHtml(markdown, Pipeline);

        html = Regex.Replace(html,
            @"<pre[^>]*>\s*<code[^>]*class=""[^""]*mermaid[^""]*""[^>]*>(.*?)</code>\s*</pre>",
            m =>
            {
                var codeHtml = m.Groups[1].Value;
                var decoded = WebUtility.HtmlDecode(codeHtml);
                return $"<div class=\"mermaid\">{decoded}</div>";
            },
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        return new Markdown
        {
            Meta = meta,
            Html = html
        };
    }
}
