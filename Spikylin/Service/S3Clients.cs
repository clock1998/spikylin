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
            AuthenticationRegion = "garage"
        };
        SpikylinS3 = new AmazonS3Client(
            new BasicAWSCredentials(
                options.SpikylinS3Bucket.AccessId, 
                options.SpikylinS3Bucket.AccessSecret), clientConfig);
    }

    public IAmazonS3 SpikylinS3 { get; }

    public void Dispose()
    {
        SpikylinS3.Dispose();
    }
}
