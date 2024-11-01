using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    // Handles rate limiting logic
    public class RateLimiter
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private int _remainingRequests = 100;
        private DateTime _rateLimitReset = DateTime.UtcNow.AddMinutes(10);

        public async Task WaitForAvailability()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_remainingRequests <= 0)
                {
                    var waitTime = _rateLimitReset - DateTime.UtcNow;
                    if (waitTime > TimeSpan.Zero)
                    {
                        await Task.Delay(waitTime);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void UpdateLimits(IDictionary<string, string> headers)
        {
            if (headers.TryGetValue("x-ratelimit-remaining", out var remaining) &&
                headers.TryGetValue("x-ratelimit-reset", out var reset) &&
                int.TryParse(remaining, out var remainingValue) &&
                double.TryParse(reset, out var resetValue))
            {
                Interlocked.Exchange(ref _remainingRequests, remainingValue);
                _rateLimitReset = DateTime.UtcNow.AddSeconds(resetValue);
            }
        }
    }
} 