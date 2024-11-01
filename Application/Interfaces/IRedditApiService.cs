using System.Collections.Generic;
using System.Threading.Tasks;
using SharedKernel.Dtos;

namespace Application.Interfaces
{
    public interface IRedditApiService
    {
        Task<RedditApiResult> GetLatestPostsAsync(string subreddit);
    }
} 