using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        public List<string> Tags { get; set; } = new List<string>();
        public bool Published { get; set; } = true;
        public bool Featured { get; set; } = false;
    }


    public class MarkdigMarkdownParser
    {
        public static Markdown Parse(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()       // <-- enable front-matter parsing
                .UseAdvancedExtensions()
                .Build();

            // 1) parse to MarkdownDocument
            var document = Markdig.Markdown.Parse(markdown, pipeline);

            // 2) extract the first YAML block
            var yamlBlock = document
                .Descendants<YamlFrontMatterBlock>()
                .FirstOrDefault();

            var meta = new MarkdownMetadata();
            if (yamlBlock != null)
            {
                // the content lines of the block
                var lines = yamlBlock.Lines.Lines
                    .Select(l => l.Slice.ToString());
                var yaml = string.Join("\n", lines);
                // 4. Deserialize YAML to your typed object
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                meta = deserializer.Deserialize<MarkdownMetadata>(yaml);
                meta.Tags ??= new List<string>();
            }

            // 3) render HTML (ignores the front-matter block by default)
            var html = Markdig.Markdown.ToHtml(markdown, pipeline);

            return new Markdown
            {
                Meta = meta,
                Html = html
            };
        }
    }
}

