namespace Spikylin.Service;

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
