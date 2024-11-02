using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Dtos;
using SharedKernel.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Application.Options;

namespace Tests.Services
{
    public class RedditStatsServiceTests : IDisposable
    {
        private readonly Mock<IRedditApiService> _redditApiService;
        private readonly Mock<ILogger<RedditStatsService>> _logger;
        private readonly Mock<IRateLimiter> _rateLimiter;
        private readonly RedditStatsService _sut;
        private readonly CancellationTokenSource _cts;

        public RedditStatsServiceTests()
        {
            _redditApiService = new Mock<IRedditApiService>();
            _logger = new Mock<ILogger<RedditStatsService>>();
            _rateLimiter = new Mock<IRateLimiter>();
            _sut = new RedditStatsService(
                _redditApiService.Object,
                _rateLimiter.Object,
                _logger.Object,
                new SubredditTrackerOptions { ProcessorCount = 1 }
            );
            _cts = new CancellationTokenSource();
            _cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            SetupDefaultMocks();
        }

        private void SetupDefaultMocks()
        {
            _redditApiService
                .Setup(x => x.GetLatestPostsAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new RedditApiResult(Array.Empty<RedditPostDto>(), new Dictionary<string, string>()));

            _rateLimiter
                .Setup(x => x.WaitForAvailability())
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task StartTracking_WithNullSubreddit_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _sut.StartTracking(null!, _cts.Token)
            );
        }


        [Fact]
        public async Task TrackPost_ShouldCreateTrackerIfNotExists()
        {
            // Arrange
            var post = new RedditPostDto { Id = "123", Subreddit = "testsubreddit", Author = "testauthor" };

            try
            {
                // Act
                await _sut.TrackPost(post, _cts.Token);

                // Assert
                await _sut.TrackPost(post, _cts.Token); // Second call should use same tracker
                _redditApiService.Verify(
                    x => x.GetLatestPostsAsync(
                        It.Is<string>(s => s == post.Subreddit),
                        It.IsAny<int>()
                    ),
                    Times.Never()
                );
            }
            finally
            {
                // Cleanup
                _sut.Dispose();
            }
        }

        [Fact]
        public async Task TrackPost_WithNullPost_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _sut.TrackPost(null!, _cts.Token)
            );
        }

        [Fact]
        public async Task TrackPost_WithNullSubreddit_ThrowsArgumentNullException()
        {
            // Arrange
            var post = new RedditPostDto { Subreddit = null };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _sut.TrackPost(post, _cts.Token)
            );
        }

        public void Dispose()
        {
            _cts.Dispose();
            _sut.Dispose();
        }
    }
}