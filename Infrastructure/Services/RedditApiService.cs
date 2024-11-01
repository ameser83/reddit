using Microsoft.Extensions.Configuration;
using Reddit;
using SharedKernel.Interfaces;
using SharedKernel.Dtos;

namespace Infrastructure.Services
{
    public class RedditApiService : IRedditApiService
    {
        private readonly RedditClient _redditClient;
        private readonly IConfiguration _configuration;
        private DateTime _lastRequestTime;
        private static readonly TimeSpan RateLimitDelay = TimeSpan.FromSeconds(2); // Reddit's rate limit

        public RedditApiService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Get credentials from configuration
            var redditSettings = _configuration.GetSection("RedditApi");
            
            _redditClient = new RedditClient(
                appId: redditSettings["AppId"],       
                appSecret: redditSettings["AppSecret"], 
                refreshToken: null,                    
                accessToken: redditSettings["AccessToken"],
                userAgent: redditSettings["UserAgent"]
            );
            var user = _redditClient.Account.Me;
            Console.WriteLine($"Connected to Reddit as {user.Name}");

            _lastRequestTime = DateTime.MinValue;
        }

        public async Task<RedditApiResult> GetLatestPostsAsync(string subreddit, int limit = 25)
        {
            await EnforceRateLimit();

            var posts = await Task.Run(() => 
                _redditClient.Subreddit(subreddit).Posts.New
                    .Take(limit)
                    .Select(post => new RedditPostDto
                    {
                        Id = post.Id,
                        Title = post.Title,
                        Content = post.Listing.SelfText,
                        Author = post.Author,
                        Url = post.Permalink,
                        CreatedUtc = post.Created,
                        Subreddit = post.Subreddit,
                        Score = post.Score.ToString()
                    })
                    .ToList());

            _lastRequestTime = DateTime.UtcNow;
            return new RedditApiResult(posts, new Dictionary<string, string>());
        }

        private async Task EnforceRateLimit()
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest < RateLimitDelay)
            {
                await Task.Delay(RateLimitDelay - timeSinceLastRequest);
            }
        }

    }
} 