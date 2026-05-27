using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.RegularExpressions;
using System.Net;

namespace Spikylin.Infrastructure.Helper
{
    public class Markdown
    {
        public MarkdownMetadata Meta { get; init; } = new MarkdownMetadata();
        public string Html { get; init; } = string.Empty;
    }
    public class MarkdownMetadata
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Date { get; set; }
        public DateTime Updated { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public bool Published { get; set; } = true;
        public bool Featured { get; set; } = false;
    }


    public class MarkdigMarkdownParser
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UseAdvancedExtensions()
            .Build();

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public static Markdown Parse(string markdown, string? filePath = null)
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

            // Convert fenced mermaid code blocks into <div class="mermaid"> so
            // client-side Mermaid can detect and render them. Markdig renders
            // fenced code blocks as <pre><code class="language-mermaid">..</code></pre>,
            // so replace that with an unencoded div containing the original code.
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
}

