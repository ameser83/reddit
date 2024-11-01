using SharedKernel.Dtos;

namespace SharedKernel.Interfaces;

public interface IRedditService
{
    Task<RedditApiResult> GetLatestPostsAsync(string subreddit, int limit = 25);
}