namespace Application.Services;
using SharedKernel.Dtos;
using SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;

public class RedditService : IRedditService
{
    private readonly IRedditApiService _redditApiService;
    private readonly ILogger<RedditService> _logger;

    public RedditService(
        IRedditApiService redditApiService,
        ILogger<RedditService> logger)
    {
        _redditApiService = redditApiService;
        _logger = logger;
    }

    public async Task<RedditApiResult> GetLatestPostsAsync(
        string subreddit, 
        int limit = 25,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(subreddit);

        try
        {
            _logger.LogInformation("Fetching {Limit} posts from r/{Subreddit}", limit, subreddit);
            return await _redditApiService.GetLatestPostsAsync(subreddit, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch posts from r/{Subreddit}", subreddit);
            throw new Exception($"Error fetching posts from r/{subreddit}", ex);
        }
    }
}