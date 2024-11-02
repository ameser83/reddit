using SharedKernel.Interfaces;
using SharedKernel.Dtos;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Application.Options;

namespace Application.Services
{
    public class RedditStatsService : IRedditStatsService, IDisposable
    {
        private readonly IRedditApiService _redditApiService;
        private readonly ILogger<RedditStatsService> _logger;
        private readonly IRateLimiter _rateLimiter;
        private readonly ConcurrentDictionary<string, SubredditTracker> _trackers;
        private readonly SubredditTrackerOptions _trackerOptions;
        private bool _disposed;

        public RedditStatsService(
            IRedditApiService redditApiService,
            IRateLimiter rateLimiter,
            ILogger<RedditStatsService> logger,
            SubredditTrackerOptions? trackerOptions = null)
        {
            _redditApiService = redditApiService;
            _rateLimiter = rateLimiter;
            _logger = logger;
            _trackerOptions = trackerOptions ?? new SubredditTrackerOptions();
            _trackers = new ConcurrentDictionary<string, SubredditTracker>();
        }

        public async Task StartTracking(string subreddit, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(subreddit);

            var tracker = _trackers.GetOrAdd(subreddit, CreateTracker);
            await tracker.StartTracking(cancellationToken);
        }

        private SubredditTracker CreateTracker(string subreddit) =>
            new(subreddit, _redditApiService, _rateLimiter, _logger, _trackerOptions);

        public async Task TrackPost(RedditPostDto post, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(post);
            ArgumentException.ThrowIfNullOrEmpty(post.Subreddit);

            var tracker = _trackers.GetOrAdd(post.Subreddit, CreateTracker);
            await tracker.ProcessPostAsync(post);
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            foreach (var tracker in _trackers.Values)
            {
                if (tracker is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            
            _trackers.Clear();
            _disposed = true;
        }
    }
}