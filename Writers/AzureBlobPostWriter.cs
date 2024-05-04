using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Serilog;
using WordPressPCL.Models;

public class AzureBlobPostWriter : IPostWriter
{
    private readonly ILogger           _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string            _containerName;

    private                 BlobContainerClient _containerClient;
    private static readonly string              _connectionString  = "DefaultEndpointsProtocol=https;AccountName=yourAccountName;AccountKey=yourAcountKey;EndpointSuffix=core.windows.net";
    private static readonly string              _blobContainerName = "wp-archive";
    
    public AzureBlobPostWriter(ILogger logger)
    {
        _logger = logger;
        _logger.Debug("Create client for {0}", _blobContainerName);
        _containerClient = new BlobContainerClient(_connectionString, _blobContainerName);
    }

    public async Task WritePost(Post post, DateTime day, string postTags)
    {
        _logger.Debug("Write post '{0}' to Azure Blob Storage", post.Slug);
        var monthPath = Path.Combine(day.Year.ToString(), day.Month.ToString()).Replace("\\", "/");
        var dayPath   = Path.Combine(monthPath, day.Day.ToString()).Replace("\\", "/");
        
        _logger.Debug("Get blob html client for {0}/{1}", _blobContainerName, $"{dayPath}");
        var blobHtmlClient  = _containerClient.GetBlobClient($"{dayPath}/{post.Slug}.html");
        var blobHtmlOptions = new BlobHttpHeaders() { ContentType = "text/html" };
        _logger.Debug("Writing post {0} to {1}", post.Slug, $"{_blobContainerName}/{monthPath}/{dayPath}/{post.Slug}.html");
        await blobHtmlClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(post.Content.Rendered)), blobHtmlOptions);
        
        if (string.IsNullOrEmpty(postTags)) return;
        _logger.Debug("Get blob tag client for {0}/{1}", _blobContainerName, $"{dayPath}");
        var blobTagClient  = _containerClient.GetBlobClient($"{dayPath}/{post.Slug}.tags");
        var blobTagOptions = new BlobHttpHeaders() { ContentType = "text/plain" };
        _logger.Debug("Writing tags {0} to {1}", post.Slug, $"{_blobContainerName}/{dayPath}/{post.Slug}.tags");
        await blobTagClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes( postTags)), blobTagOptions);
    }
}