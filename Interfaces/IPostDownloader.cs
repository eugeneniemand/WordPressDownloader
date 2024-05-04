using WordPressPCL.Models;

public interface IPostDownloader
{
    Task DownloadPost( Post post, DateTime day, List<Tag> tags);
}