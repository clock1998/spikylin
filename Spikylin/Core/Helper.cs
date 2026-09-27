
namespace Spikylin.Core
{
    public static class Helper
    {
        private static readonly string[] ImageExtensions = { ".avif", ".gif", ".jpeg", ".jpg", ".png", ".webp" };
        public static string BuildThumbnailKey(string prefix, string sourceKey)
        {
            var relativeKey = sourceKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? sourceKey[prefix.Length..]
                : sourceKey;
            relativeKey = relativeKey.Replace("gallery", "gallery-thumbnail");
            var extension = Path.GetExtension(sourceKey);

            return string.IsNullOrEmpty(extension)
                ? $"{relativeKey}.webp"
                : $"{relativeKey[..^extension.Length]}.webp";
        }
        public static Uri BuildUri(string baseurl, string key)
        {
            var endpoint = baseurl.TrimEnd('/');
            var escapedKey = string.Join('/', key.Split('/').Select(Uri.EscapeDataString));
            return new Uri($"{endpoint}/{escapedKey}", UriKind.Absolute);
        }
        public static bool IsImage(string key) => ImageExtensions.Contains(Path.GetExtension(key), StringComparer.OrdinalIgnoreCase);
    }
}
