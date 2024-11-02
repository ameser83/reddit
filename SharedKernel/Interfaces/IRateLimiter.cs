public interface IRateLimiter
{
    Task WaitForAvailability();
    void UpdateLimits(IDictionary<string, string> headers);
}