using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayApi.Services;

public interface IBannerStorage
{
    Task<string> Upload(int eventId, Stream content, string contentType);
    Task<(Stream Content, string ContentType)> Download(string url);
}

// Only the API references Azure. The MVC website calls our banner endpoint.
public class BannerStorage : IBannerStorage
{
    private readonly IConfiguration _configuration;
    public BannerStorage(IConfiguration configuration) { _configuration = configuration; }

    private BlobContainerClient Container()
    {
        var connection = _configuration["AzureStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Banner storage has not been configured.");
        return new BlobContainerClient(connection, _configuration["AzureStorage:Container"] ?? "event-banners");
    }

    public async Task<string> Upload(int eventId, Stream content, string contentType)
    {
        var container = Container();
        await container.CreateIfNotExistsAsync(PublicAccessType.None);
        var extension = contentType == "image/png" ? ".png" : ".jpg";
        var blob = container.GetBlobClient($"event-{eventId}-{Guid.NewGuid():N}{extension}");
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });
        // Save a permanent URL, never a connection string or expiring access token.
        return blob.Uri.AbsoluteUri;
    }

    public async Task<(Stream Content, string ContentType)> Download(string url)
    {
        var container = Container();
        var name = new Uri(url).Segments.Last();
        var result = await container.GetBlobClient(name).DownloadStreamingAsync();
        return (result.Value.Content, result.Value.Details.ContentType);
    }
}
