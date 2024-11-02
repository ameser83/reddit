using SharedKernel.Dtos;

namespace SharedKernel.Interfaces;

public interface IRedditStatsService
{
    Task TrackPost(RedditPostDto post, CancellationToken cancellationToken);
    Task StartTracking(string subreddit, CancellationToken cancellationToken);
}