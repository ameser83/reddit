public class RedditPost
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string? Subreddit { get; set; }
    public string? Score { get; set; }
}