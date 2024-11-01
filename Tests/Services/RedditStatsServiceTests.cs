using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Dtos;
using SharedKernel.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services
{
    public class RedditStatsServiceTests
    {
        private readonly Mock<IRedditApiService> _redditApiService;
        private readonly Mock<ILogger<RedditStatsService>> _logger;
        private readonly RedditStatsService _sut;
        private readonly CancellationTokenSource _cts;

        public RedditStatsServiceTests()
        {
            _redditApiService = new Mock<IRedditApiService>();
            _logger = new Mock<ILogger<RedditStatsService>>();
            _sut = new RedditStatsService(_redditApiService.Object, _logger.Object);
            _cts = new CancellationTokenSource();
            _cts.CancelAfter(TimeSpan.FromSeconds(5)); // Timeout after 5 seconds
            
            // Setup default mock behavior
            _redditApiService
                .Setup(x => x.GetLatestPostsAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new RedditApiResult(Array.Empty<RedditPostDto>(), new Dictionary<string, string>()));
        }


        [Fact]
        public async Task StopTracking_WhenTrackerExists_ShouldRemoveTracker()
        {
            // Arrange
            var subreddit = "testsubreddit";
            var shortTimeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            try
            {
                // Start tracking (expected to be cancelled)
                await Assert.ThrowsAsync<OperationCanceledException>(
                    async () => await _sut.StartTracking(subreddit, shortTimeoutCts.Token)
                );

                // Act
                await _sut.StopTracking(subreddit);

                // Assert - Starting again should create a new tracker
                shortTimeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                await Assert.ThrowsAsync<OperationCanceledException>(
                    async () => await _sut.StartTracking(subreddit, shortTimeoutCts.Token)
                );

                _redditApiService.Verify(x => x.GetLatestPostsAsync(
                    It.Is<string>(s => s == subreddit), 
                    It.IsAny<int>()), 
                    Times.Exactly(2));
            }
            finally
            {
                await _sut.StopTracking(subreddit);
            }
        }

        [Fact]
        public async Task TrackPost_ShouldCreateTrackerIfNotExists()
        {
            // Arrange
            var post = new RedditPostDto { Subreddit = "testsubreddit" };

            try
            {
                // Act
                await _sut.TrackPost(post);

                // Assert
                await _sut.TrackPost(post); // Second call should use same tracker
                _redditApiService.Verify(x => x.GetLatestPostsAsync(
                    It.Is<string>(s => s == post.Subreddit), 
                    It.IsAny<int>()), 
                    Times.Never());
            }
            finally
            {
                await _sut.StopTracking(post.Subreddit);
            }
        }

        [Fact]
        public async Task TrackPost_WithNullSubreddit_ShouldNotThrow()
        {
            // Arrange
            var post = new RedditPostDto { Subreddit = null };

            // Act & Assert
            var exception = await Record.ExceptionAsync(() => _sut.TrackPost(post));
            Assert.Null(exception);
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}