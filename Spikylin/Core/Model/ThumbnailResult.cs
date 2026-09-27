namespace Spikylin.Core.Model
{
    public sealed record ThumbnailResult(byte[] Content, PhotoMetadata photoMetadata, string ContentType);
}
