using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharedKernel.Dtos;
using SharedKernel.Interfaces;
using Application.Options;

namespace Application.Services
{
    public class SubredditTracker
    {
        private readonly string _subreddit;
        private readonly IRedditApiService _redditApiService;
        private readonly IRateLimiter _rateLimiter;
        private readonly ILogger _logger;
        private readonly SubredditTrackerOptions _options;
        private readonly ConcurrentDictionary<string, PostStats> _postStats;
        private readonly Channel<RedditPostDto> _postProcessingChannel;
        private CancellationTokenSource? _cancellationTokenSource;
        private volatile bool _isTracking;

        public SubredditTracker(
            string subreddit,
            IRedditApiService redditApiService,
            IRateLimiter rateLimiter,
            ILogger logger,
            SubredditTrackerOptions? options = null)
        {
            _subreddit = subreddit;
            _redditApiService = redditApiService;
            _rateLimiter = rateLimiter;
            _logger = logger;
            _options = options ?? new SubredditTrackerOptions();
            _postStats = new ConcurrentDictionary<string, PostStats>();
            _postProcessingChannel = Channel.CreateUnbounded<RedditPostDto>(
                new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });
        }

        public async Task StartTracking(CancellationToken cancellationToken)
        {
            if (_isTracking)
            {
                return;
            }

            _isTracking = true;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            try
            {
                await StartProcessors(_cancellationTokenSource.Token);
            }
            finally
            {
                _isTracking = false;
            }
        }

        private async Task StartProcessors(CancellationToken token)
        {
            var processors = Enumerable.Range(0, _options.ProcessorCount)
                .Select(_ => ProcessPostsAsync(token))
                .ToList();

            var fetcher = FetchAndProcessPosts(token);
            await Task.WhenAll(processors.Concat(new[] { fetcher }));
        }

        private async Task FetchAndProcessPosts(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _rateLimiter.WaitForAvailability();
                    var result = await _redditApiService.GetLatestPostsAsync(_subreddit);
                    _rateLimiter.UpdateLimits(result.Headers);
                    var posts = result.Posts;

                    foreach (var post in posts)
                    {
                        await _postProcessingChannel.Writer.WriteAsync(post, token);
                    }

                    _logger.LogInformation("Fetched {Count} posts from r/{Subreddit}", 
                        posts.Count(), _subreddit);

                    await Task.Delay(TimeSpan.FromSeconds(2), token);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error fetching posts from r/{Subreddit}", _subreddit);
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                }
            }
        }

        private async Task ProcessPostsAsync(CancellationToken token)
        {
            await foreach (var post in _postProcessingChannel.Reader.ReadAllAsync(token))
            {
                try
                {
                    var stats = _postStats.GetOrAdd(post.Id, _ => new PostStats(post.Id));
                    await stats.UpdateStats(post);
                    LogPostStats(stats, _subreddit);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing post {PostId}", post.Id);
                }
            }
        }

        private void LogPostStats(PostStats stats, string subreddit)
        {
            _logger.LogInformation(
                "Post {PostId} processed. Score: {Score}, Author: {Author} for r/{Subreddit}",
                stats.PostId,
                stats.Score,
                stats.Author,
                subreddit
            );
        }

        public async Task ProcessPostAsync(RedditPostDto post)
        {
            await _postProcessingChannel.Writer.WriteAsync(post);
        }
    }
} 