public class ImageDownloader
{
    
    public async Task<byte[]> DownloadImage(string imageUrl)
    {
        using var httpClient = new HttpClient();
        var       getImage   = await httpClient.GetAsync(imageUrl);
        return await getImage.Content.ReadAsByteArrayAsync();
    }
}