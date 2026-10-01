namespace Spikylin.Features.Shared
{
    public static class Helper
    {
        private static readonly string[] ImageExtensions = { ".avif", ".gif", ".jpeg", ".jpg", ".png", ".webp" };
        public static Uri BuildUri(string baseurl, string key)
        {
            var endpoint = baseurl.TrimEnd('/');
            var escapedKey = string.Join('/', key.Split('/').Select(Uri.EscapeDataString));
            return new Uri($"{endpoint}/{escapedKey}", UriKind.Absolute);
        }
        public static bool IsImage(string key) => ImageExtensions.Contains(Path.GetExtension(key), StringComparer.OrdinalIgnoreCase);
    }
}
