using Amazon.Runtime;
using Amazon.S3;

namespace Spikylin.Service;

public sealed class S3Clients : IDisposable
{
    public S3Clients(S3PhotoOptions options)
    {
        var endpoint = new Uri(options.Endpoint);
        var clientConfig = new AmazonS3Config
        {
            ServiceURL = $"{endpoint.Scheme}://{endpoint.Authority}",
            ForcePathStyle = true,
            Timeout = TimeSpan.FromSeconds(30),
        };

        Public = new AmazonS3Client(new AnonymousAWSCredentials(), clientConfig);
        Thumbnails = new AmazonS3Client(
            new BasicAWSCredentials(options.ThumbnailBucket.AccessId, options.ThumbnailBucket.AccessSecret),
            clientConfig);
    }

    public IAmazonS3 Public { get; }

    public IAmazonS3 Thumbnails { get; }

    public void Dispose()
    {
        Public.Dispose();
        Thumbnails.Dispose();
    }
}
