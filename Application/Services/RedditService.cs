namespace Application.Services;
using SharedKernel.Dtos;
using SharedKernel.Interfaces;

public class RedditService : IRedditService
{
    private readonly IRedditApiService _redditApiService;

    public RedditService(IRedditApiService redditApiService)
    {
        _redditApiService = redditApiService;
    }

    public async Task<RedditApiResult> GetLatestPostsAsync(string subreddit, int limit = 25)
    {
        if (string.IsNullOrEmpty(subreddit))
        {
            throw new ArgumentException("Subreddit cannot be null or empty");
        }

        try
        {
            var result = await _redditApiService.GetLatestPostsAsync(subreddit, limit);
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Error fetching posts from Reddit", ex);
        }
    }
}