using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharedKernel.Dtos;
using SharedKernel.Interfaces;

namespace Application.Services
{
    // Handles tracking for a single subreddit
    public class SubredditTracker
    {
        private readonly string _subreddit;
        private readonly IRedditApiService _redditApiService;
        private readonly RateLimiter _rateLimiter;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, PostStats> _postStats;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _trackingTask;
        private readonly Channel<RedditPostDto> _postProcessingChannel;

        public SubredditTracker(
            string subreddit,
            IRedditApiService redditApiService,
            RateLimiter rateLimiter,
            ILogger logger)
        {
            _subreddit = subreddit;
            _redditApiService = redditApiService;
            _rateLimiter = rateLimiter;
            _logger = logger;
            _postStats = new ConcurrentDictionary<string, PostStats>();
            _postProcessingChannel = Channel.CreateUnbounded<RedditPostDto>(
                new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });
        }

        public async Task StartTracking(CancellationToken cancellationToken)
        {
            if (_trackingTask != null) return;

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            // Start post processors
            var processorCount = Environment.ProcessorCount;
            var processors = Enumerable.Range(0, processorCount)
                .Select(_ => ProcessPostsAsync(token))
                .ToList();

            // Start the main tracking task
            _trackingTask = Task.Run(async () =>
            {
                try
                {
                    await FetchAndProcessPosts(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in tracking task for r/{Subreddit}", _subreddit);
                }
                finally
                {
                    _postProcessingChannel.Writer.Complete();
                }
            }, token);

            // Combine all tasks
            await Task.WhenAll(processors.Concat(new[] { _trackingTask }));
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
                    LogPostStats(stats);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing post {PostId}", post.Id);
                }
            }
        }

        public async Task StopTracking()
        {
            if (_cancellationTokenSource != null)
            {
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource = null;
            }
        }

        private void LogPostStats(PostStats stats)
        {
            _logger.LogInformation(
                "Post {PostId} processed. Score: {Score}, Author: {Author}",
                stats.PostId,
                stats.Score,
                stats.Author
            );
        }

        public async Task ProcessPostAsync(RedditPostDto post)
        {
            await _postProcessingChannel.Writer.WriteAsync(post);
        }
    }
} 