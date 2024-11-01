using System.Collections.Generic;

namespace SharedKernel.Dtos;

public record RedditApiResult(IEnumerable<RedditPostDto> Posts, IDictionary<string, string> Headers); 