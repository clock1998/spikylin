namespace Spikylin
{
    public class S3BucketOptions
    {
        public string BucketName { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string AccessId { get; set; } = string.Empty;
        public string AccessSecret { get; set; } = string.Empty;
    }


    public sealed class S3Options
    {
        public string Endpoint { get; set; } = "https://s3.spikylin.com";
        public string WebsiteEndpoint { get; set; } = "https://s3.spikylin.com";
        public List<S3BucketOptions> Buckets { get; set; } = new();

        public S3BucketOptions SpikylinS3Bucket =>
            Buckets.FirstOrDefault(bucket => string.Equals(bucket.BucketName, "spikylin-s3", StringComparison.OrdinalIgnoreCase))
            ?? new S3BucketOptions { BucketName = "spikylin-s3", Prefix = "gallery/" };
    }
}
