using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Serilog;

public class AzureBlobImageWriter : IImageWriter
{
    private readonly ILogger             _logger;
    private readonly BlobServiceClient   _blobServiceClient;
    private readonly string              _containerName;
    private readonly Regex               _filePathPattern = new(@"https?:\/\/.*stainsbury.org/wp-content/uploads/(\d+)/(\d+)/(.*)");
    
    private                 BlobContainerClient _containerClient;
    private static readonly string              _connectionString  = "DefaultEndpointsProtocol=https;AccountName=yourAccountName;AccountKey=yourAcountKey;EndpointSuffix=core.windows.net";
    private static readonly string              _blobContainerName = "wp-archive";
    
    public AzureBlobImageWriter(ILogger logger)
    {
        _logger          = logger;
        _logger.Debug("Create client for {0}", _blobContainerName);
        _containerClient = new BlobContainerClient(_connectionString, _blobContainerName);
    }

    public async Task WriteImage(string imageUrl, DateTime day, byte[] image)
    {
        _logger.Debug("Write image {0} to Azure Blob Storage", imageUrl);
        var monthPath = Path.Combine(day.Year.ToString(), day.Month.ToString()).Replace("\\", "/");;
        var pathParts = _filePathPattern.Matches(imageUrl);
        var fileName  = pathParts.First().Groups[3].Value;
        _logger.Debug("Get blob client for {0}/{1}", _blobContainerName, $"{monthPath}");
        var blobClient  = _containerClient.GetBlobClient($"{monthPath}/{fileName}");
        var blobOptions = new BlobHttpHeaders() { ContentType = "image/jpeg" };
        _logger.Debug("Writing image {0} to {1}", imageUrl, $"{_blobContainerName}/{monthPath}/{fileName}");
        await blobClient.UploadAsync(new MemoryStream(image), blobOptions);
    }


    
    
}