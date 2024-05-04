using WordPressPCL.Models;

public interface IPostWriter
{
    Task WritePost(Post post, DateTime day, string postTags);
}