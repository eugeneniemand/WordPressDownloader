using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Serilog;
using WordPressPCL.Models;

public class AzureBlobLogFileWriter 
{
    private readonly ILogger           _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string            _containerName;

    private                 BlobContainerClient _containerClient;
    private static readonly string              _connectionString  = "DefaultEndpointsProtocol=https;AccountName=yourAccountName;AccountKey=yourAcountKey;EndpointSuffix=core.windows.net";
    private static readonly string              _blobContainerName = "wp-archive";
    
    public AzureBlobLogFileWriter(ILogger logger)
    {
        _logger = logger;
        _logger.Debug("Create client for {0}", _blobContainerName);
        _containerClient = new BlobContainerClient(_connectionString, _blobContainerName);
    }

    public async Task WritePost(string logFileName, string logFileContents, DateTime day)
    {
        _logger.Debug("Write log to Azure Blob Storage");
        var monthPath = Path.Combine(day.Year.ToString(), day.Month.ToString()).Replace("\\", "/");
        
        _logger.Debug("Get blob log client for {0}/{1}", _blobContainerName, $"{monthPath}");
        var blobTagClient  = _containerClient.GetBlobClient($"{monthPath}/{logFileName}");
        var blobTagOptions = new BlobHttpHeaders() { ContentType = "text/plain" };
        _logger.Debug("Writing tags {0} to {1}", logFileName, $"{_blobContainerName}/{monthPath}/{logFileName}");
        await blobTagClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes( logFileContents)), blobTagOptions);
    }

    
}