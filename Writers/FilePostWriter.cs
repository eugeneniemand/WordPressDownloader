using System.Text.Json;
using WordPressPCL.Models;

public class FilePostWriter : IPostWriter
{
    public async Task WritePost(Post post, DateTime day, string postTags)
    {
        var monthPath = Path.Combine(Environment.CurrentDirectory, day.Year.ToString(), day.Month.ToString());
        var dayPath   = Path.Combine(monthPath, day.Day.ToString());
        Directory.CreateDirectory(dayPath);
        
        await File.WriteAllTextAsync(Path.Combine( dayPath, post.Slug + ".html"), post.Content.Rendered);
        if (string.IsNullOrEmpty(postTags)) return;
        await File.WriteAllTextAsync(Path.Combine( dayPath, post.Slug + ".tags"),  postTags);
    }
}