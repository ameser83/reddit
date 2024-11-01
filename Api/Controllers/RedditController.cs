using SharedKernel.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedditController : ControllerBase
{
    private readonly IRedditService _redditService;
    private readonly IRedditStatsService _statsService;
    private readonly ILogger<RedditController> _logger;

    public RedditController(
        IRedditService redditService, 
        IRedditStatsService statsService,
        ILogger<RedditController> logger)
    {
        _redditService = redditService;
        _statsService = statsService;
        _logger = logger;
    }

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts([FromQuery] string subreddit, [FromQuery] int limit = 25)
    {
        var posts = await _redditService.GetLatestPostsAsync(subreddit, limit);
        return Ok(posts);
    }

    [HttpPost("track/{subreddit}")]
    public async Task<IActionResult> StartTracking(string subreddit)
    {
        await _statsService.StartTracking(subreddit, HttpContext.RequestAborted);
        return Ok($"Started tracking r/{subreddit}");
    }
}