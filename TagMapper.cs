using WordPressPCL.Models;

namespace RobDownloader;

public class TagMapper() : ITagMapper
{
    public string GetPostTags(Post post, List<Tag> tags)
    {
        var postTags = tags.Where(t => post.Tags.Contains(t.Id)).Select(t => t.Name.Replace(";"," ")).ToList();
        return string.Join(";", postTags);
    }
}