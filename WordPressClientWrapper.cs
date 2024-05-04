using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;

public class WordPressClientWrapper
{
    private WordPressClient _client;

    public WordPressClientWrapper(string baseUrl, string username, string password)
    {
        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(baseUrl)
        };
        _client = new WordPressClient(httpClient);
        _client.Auth.UseBasicAuth(username, password);
    }

    public async Task<List<Post>> GetPosts(DateTime day)
    {
        var queryBuilder = new PostsQueryBuilder
        {
            After   = day,
            Before  = day.AddDays(1),
            PerPage = 100
        };
        return await _client.Posts.QueryAsync(queryBuilder, useAuth: true);
    }
    
    public async Task<MediaItem> GetMedia(Post post)
    {
        
        return await _client.Media.GetByIDAsync(post.Guid, useAuth: true);
    }
    
    public async Task<List<Tag>> GetTags()
    {
        return await _client.Tags.GetAllAsync(useAuth: true);
    }
}