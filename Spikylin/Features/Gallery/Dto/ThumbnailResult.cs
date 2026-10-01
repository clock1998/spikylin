namespace Spikylin.Features.Gallery.Dto
{
    public sealed record ThumbnailResult(byte[] Content, PhotoMetadata PhotoMetadata, string ContentType);
}
