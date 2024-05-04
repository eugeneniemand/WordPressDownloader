using System;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using RobDownloader;
using Serilog;
using Serilog.Events;
using WordPressPCL.Client;
using WordPressPCL.Models;

class Program
{
    static async Task Main(string[] args)
    {
        var storageMode = StorageMode.AzureBlob;
        var startDate   = new DateTime(2019, 1, 1);
        while (startDate < new DateTime(2025, 1, 1))
        {
            var endDate     = startDate.AddMonths(1).AddDays(-1);
            var logFileName = $"RobDownloader{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.log";

            await DownloadDataRange(logFileName, storageMode, startDate, endDate);
            
            startDate = startDate.AddMonths(1);
        }
    }

    private static async Task DownloadDataRange(string logFileName, StorageMode storageMode, DateTime startDate, DateTime endDate)
    {
        // Create service collection
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, logFileName, storageMode);

        // Create service provider
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Resolve services from DI container
        var clientWrapper = serviceProvider.GetRequiredService<WordPressClientWrapper>();
        var logger        = serviceProvider.GetRequiredService<ILogger>();
        
        logger.Information("Downloading posts between {0:dd/MM/yyyy} and {1:dd/MM/yyyy}", startDate, endDate);
        var dates = Enumerable.Range(0, (endDate - startDate).Days).Select(d => startDate.AddDays(d));

        var exceptionCount = 0;
        var pipeline = new ResiliencePipelineBuilder()
                       .AddRetry(new RetryStrategyOptions()
                       {
                           MaxRetryAttempts = 2,
                           BackoffType      = DelayBackoffType.Constant,
                           Delay            = TimeSpan.FromSeconds(3),
                           OnRetry = retryPredicateArguments =>
                           {
                               logger.Warning("Current Attempt: {count} Exception: {ex}", retryPredicateArguments.AttemptNumber, retryPredicateArguments.Outcome.Exception );
                               return ValueTask.CompletedTask;
                           }
                       } ) // Add retry using the default options
                       .AddTimeout(TimeSpan.FromSeconds(10)) // Add 10 seconds timeout
                       .Build(); // Builds the resilience pipeline
        
        var tags = await pipeline.ExecuteAsync(async token => await clientWrapper.GetTags());
        logger.Debug("Found {0} tags", tags.Count);
        
        var dateTasks = dates.Select(async date =>
        {
            try
            {
                logger.Debug("Downloading posts for {0:dd/MM/yyyy}", date);
                var posts = await pipeline.ExecuteAsync(async token => await clientWrapper.GetPosts(date));

                logger.Debug("Found {0} posts", posts.Count);

                var postDownloader = serviceProvider.GetRequiredService<PostDownloader>();

                var postTasks = posts.Select(async post =>
                {
                    await pipeline.ExecuteAsync(async token => await postDownloader.DownloadPost(post, date, tags));
                    
                });

                await Task.WhenAll(postTasks);
            }
            catch (Exception e)
            {
                Interlocked.Increment(ref exceptionCount);
                logger.Error(e, "An error occurred while downloading posts for {0:dd/MM/yyyy}", date);
            }
        });

        await Task.WhenAll(dateTasks);
        logger.Information($"Downloading Completed with {exceptionCount} exceptions.");
        
        if (storageMode == StorageMode.AzureBlob)
        {
            var             logFileWriter   = serviceProvider.GetRequiredService<AzureBlobLogFileWriter>();
            await using var fileStream      = new FileStream(logFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var       textReader      = new StreamReader(fileStream);
            var             logFileContents = await textReader.ReadToEndAsync();
            await logFileWriter.WritePost(logFileName, logFileContents, startDate);
        }
    }

    private static void ConfigureServices(IServiceCollection services, string logFileName, StorageMode storageMode)
    {
        // Create logger
        var logger = new LoggerConfiguration()
                     .MinimumLevel.Debug()
                     .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                     .WriteTo.File(logFileName)
                     .CreateLogger();

        // Register your services here
        services.AddSingleton(new WordPressClientWrapper("https://yourblog.org/wp-json/", "eugeneniemand", "application_password_here"));
        services.AddTransient<PostDownloader>();
        services.AddTransient<ImageDownloader>();
        services.AddTransient<PostImageExtractor>();
        services.AddTransient<ITagMapper, TagMapper>();

        switch (storageMode)
        {
            case StorageMode.File:
                services.AddTransient<IPostWriter, FilePostWriter>();
                services.AddTransient<IImageWriter, FileImageWriter>();
                break;
            case StorageMode.AzureBlob:
                services.AddTransient<IPostWriter, AzureBlobPostWriter>();
                services.AddTransient<IImageWriter, AzureBlobImageWriter>();
                services.AddSingleton(new AzureBlobLogFileWriter(logger));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(storageMode), storageMode, null);
        }
        services.AddSingleton<ILogger>(logger);
    }
    
    public enum StorageMode
    {
        File,
        AzureBlob
    }
}