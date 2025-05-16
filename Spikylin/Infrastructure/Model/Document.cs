using Spikylin.Infrastructure.Helper;

namespace Spikylin.Infrastructure.Model
{
    public class Post
    {
        public string FileName { get; set; } = string.Empty;
        public Markdown Markdown { get; set; } = new Markdown();
    }
}
