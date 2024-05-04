using Polly;
using Polly.Retry;
using Serilog;
using WordPressPCL.Models;

namespace RobDownloader;

public class PostDownloader(IPostWriter postWriter, IImageWriter imageWriter, ITagMapper tagMapper, PostImageExtractor imageExtractor, ImageDownloader imageDownloader, ILogger logger)
    : IPostDownloader
{
    public async Task DownloadPost(Post post, DateTime day, List<Tag> tags)
    {
        
            var postId = $"{day.Year}-{day.Month}-{day.Day}-{post.Slug}";
            try
            {
                logger.Information("'{postId}' Processing Started", postId);
                logger.Debug("'{postId}' Mapping Tags", postId);
                var postTags = tagMapper.GetPostTags(post, tags);

                logger.Debug("'{postId}' Writing Post", postId);
                await postWriter.WritePost(post, day, postTags);

                logger.Debug("'{postId}' Extracting image URLs", postId);
                var imageUrls = imageExtractor.ExtractImageUrls(post);

                logger.Debug("'{postId}' Found {imageCount} images", postId, imageUrls.Count);

                var imageTasks = imageUrls.Select(imageUrl => DownloadAndWriteImage(postId, day, imageUrl)).ToList();
                await Task.WhenAll(imageTasks);

                logger.Information("'{postId}' Processing completed. Images:{imageCount} Tags:'{tags}'", postId, imageUrls.Count, postTags);
            }
            catch (Exception e)
            {
                logger.Error(e, "'{postId}' An error occurred", postId);
                throw;
            }
    }
    
    private async Task DownloadAndWriteImage(string postId, DateTime day, string imageUrl)
    {
        logger.Debug("'{postId}' Downloading image {imageUrl}", postId, imageUrl);
        var image = await imageDownloader.DownloadImage(imageUrl);

        logger.Debug("'{postId}' Writing image {imageUrl}", postId, imageUrl);
        await imageWriter.WriteImage(imageUrl, day, image);
    }
}