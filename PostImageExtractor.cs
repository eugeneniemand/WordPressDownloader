using System.Text.RegularExpressions;
using WordPressPCL.Models;

public class PostImageExtractor
{
    private readonly Regex _imageUrlPattern = new(@"https?:\/\/[^""'\s\?]*stainsbury\.org\/wp-content[^""'\s\?]*");

    public List<string> ExtractImageUrls(Post post)
    {
        var urls = new List<string>();
        foreach (Match imageUrl in _imageUrlPattern.Matches(post.Content.Rendered))
            urls.Add(imageUrl.Value.Replace("data-orig-file=\"", ""));
        return urls.Distinct().ToList();
    }
}