using SharedKernel.Dtos;

public class PostStats
{
    public string PostId { get; }
    public int Score { get; private set; }
    public string? Author { get; private set; }

    public PostStats(string postId)
    {
        PostId = postId;
    }

    public Task UpdateStats(RedditPostDto post)
    {
        _ = int.TryParse(post.Score, out int score);
        Score = score;
        Author = post.Author;
        return Task.CompletedTask;
    }
}
