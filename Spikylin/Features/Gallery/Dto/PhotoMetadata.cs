namespace Spikylin.Features.Gallery.Dto
{
    public record PhotoMetadata(
        string? CameraModel,
        string? DateTime,
        string? FocalLength,
        string? Aperture,
        string? Iso,
        string? ShutterSpeed,
        string? OriginalPhotoUrl = null)
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
