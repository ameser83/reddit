using SharedKernel.Dtos;

namespace SharedKernel.Interfaces;

public interface IRedditApiService
{
    Task<RedditApiResult> GetLatestPostsAsync(string subreddit, int limit = 25);
}