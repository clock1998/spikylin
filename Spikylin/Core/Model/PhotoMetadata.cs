namespace Spikylin.Core.Model
{
    public record PhotoMetadata(
        string? CameraModel,
        string? DateTime,
        string? FocalLength,
        string? Aperture,
        string? Iso,
        string? ShutterSpeed,
        string? OriginalPhotoKey = null)
    {
        public string DisplayText => string.Join("|",
            new[]
            {
            CameraModel,
            DateTime,
            FocalLength,
            Aperture,
            Iso,
            ShutterSpeed,
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
