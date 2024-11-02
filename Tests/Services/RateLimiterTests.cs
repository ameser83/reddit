using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Services;
using Xunit;

namespace Tests.Services
{
    public class RateLimiterTests
    {
        private readonly RateLimiter _sut;

        public RateLimiterTests()
        {
            _sut = new RateLimiter();
        }

        [Fact]
        public async Task WaitForAvailability_WithRemainingRequests_ShouldNotDelay()
        {
            // Arrange
            var startTime = DateTime.UtcNow;

            // Act
            await _sut.WaitForAvailability();
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(elapsed.TotalMilliseconds < 100);
        }

        [Fact]
        public async Task WaitForAvailability_WithNoRemainingRequests_ShouldDelay()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                ["x-ratelimit-remaining"] = "0",
                ["x-ratelimit-reset"] = "1"
            };
            _sut.UpdateLimits(headers);
            var startTime = DateTime.UtcNow;

            // Act
            await _sut.WaitForAvailability();
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(elapsed.TotalSeconds >= 0.9);
        }

        [Fact]
        public void UpdateLimits_WithMissingHeaders_ShouldNotThrow()
        {
            // Arrange
            var headers = new Dictionary<string, string>();

            // Act & Assert
            var exception = Record.Exception(() => _sut.UpdateLimits(headers));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            var tasks = new List<Task>();
            var iterations = 100;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await _sut.WaitForAvailability();
                    _sut.UpdateLimits(new Dictionary<string, string>
                    {
                        ["x-ratelimit-remaining"] = "50",
                        ["x-ratelimit-reset"] = "30"
                    });
                }));
            }

            // Assert
            var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            Assert.Null(exception);
        }
    }
}