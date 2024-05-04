using WordPressPCL.Models;

public interface ITagMapper
{
    string GetPostTags(Post post, List<Tag> tags);
}