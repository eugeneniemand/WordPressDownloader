using System.Text.RegularExpressions;
using Serilog;

public class FileImageWriter : IImageWriter
{
    private readonly ILogger    _logger;
    private readonly HttpClient _httpClient      = new();
    private readonly Regex      _filePathPattern = new(@"https:\/\/.*stainsbury.org/wp-content/uploads/(\d+)/(\d+)/(.*)");

    public FileImageWriter(ILogger logger)
    {
        _logger = logger;
    }
    public async Task WriteImage(string imageUrl, DateTime day, byte[] image)
    {
        var monthPath = Path.Combine(Environment.CurrentDirectory, day.Year.ToString(), day.Month.ToString());
        Directory.CreateDirectory(monthPath);
        
        var pathParts = _filePathPattern.Matches(imageUrl);
        var fileName  = pathParts.First().Groups[3].Value;
        var path      = Path.Combine(monthPath, fileName);
        _logger.Debug("Writing image {0} to {1}", imageUrl, path);
        await File.WriteAllBytesAsync(path, image);
    }
}