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
            Assert.True(elapsed.TotalSeconds < 1); // Should return almost immediately
        }

        [Fact]
        public async Task WaitForAvailability_WithNoRemainingRequests_ShouldDelay()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                ["x-ratelimit-remaining"] = "0",
                ["x-ratelimit-reset"] = "2" // 2 second delay
            };
            _sut.UpdateLimits(headers);
            var startTime = DateTime.UtcNow;

            // Act
            await _sut.WaitForAvailability();
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(elapsed.TotalSeconds >= 1.9); // Allow small margin for timing
        }

        [Fact]
        public void UpdateLimits_ShouldUpdateRateLimitValues()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                ["x-ratelimit-remaining"] = "50",
                ["x-ratelimit-reset"] = "30"
            };

            // Act
            _sut.UpdateLimits(headers);

            // Assert - We can only test indirectly through behavior
            var task = _sut.WaitForAvailability();
            Assert.True(task.IsCompleted); // Should complete immediately since remaining > 0
        }

        [Fact]
        public void UpdateLimits_WithInvalidHeaders_ShouldNotThrow()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                ["x-ratelimit-remaining"] = "invalid",
                ["x-ratelimit-reset"] = "not a number"
            };

            // Act & Assert
            var exception = Record.Exception(() => _sut.UpdateLimits(headers));
            Assert.Null(exception);
        }
    }
}