using SharedKernel.Dtos;

namespace SharedKernel.Interfaces;

public interface IRedditStatsService
{
    void TrackPost(RedditPostDto post);
    Task StartTracking(string subreddit, CancellationToken cancellationToken);
    Task StopTracking();
}