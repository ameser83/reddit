using SharedKernel.Interfaces;
using SharedKernel.Dtos;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Application.Services
{
    public class RedditStatsService : IRedditStatsService
    {
        private readonly IRedditApiService _redditApiService;
        private readonly ILogger<RedditStatsService> _logger;
        private readonly ConcurrentDictionary<string, SubredditTracker> _subredditTrackers;
        private readonly RateLimiter _rateLimiter;

        public RedditStatsService(
            IRedditApiService redditApiService,
            ILogger<RedditStatsService> logger)
        {
            _redditApiService = redditApiService;
            _logger = logger;
            _subredditTrackers = new ConcurrentDictionary<string, SubredditTracker>();
            _rateLimiter = new RateLimiter();
        }

        public async Task StartTracking(string subreddit, CancellationToken cancellationToken)
        {
            var tracker = _subredditTrackers.GetOrAdd(subreddit, 
                s => new SubredditTracker(s, _redditApiService, _rateLimiter, _logger));
            await tracker.StartTracking(cancellationToken);
        }

        public async Task StopTracking(string subreddit)
        {
            if (_subredditTrackers.TryRemove(subreddit, out var tracker))
            {
                await tracker.StopTracking();
            }
        }

        public Task StopTracking()
        {
            throw new NotImplementedException();
        }

        public async Task TrackPost(RedditPostDto post)
        {
            if (string.IsNullOrEmpty(post.Subreddit))
            {
                _logger.LogWarning("Attempted to track post with null or empty subreddit");
                return;
            }

            var tracker = _subredditTrackers.GetOrAdd(post.Subreddit, 
                s => new SubredditTracker(s, _redditApiService, _rateLimiter, _logger));
            await tracker.ProcessPostAsync(post);
        }

        void IRedditStatsService.TrackPost(RedditPostDto post)
        {
            throw new NotImplementedException();
        }
    }
}