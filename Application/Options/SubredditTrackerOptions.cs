namespace Application.Options;

    public class SubredditTrackerOptions
    {
        public int ProcessorCount { get; set; } = Environment.ProcessorCount;
        public TimeSpan FetchInterval { get; set; } = TimeSpan.FromSeconds(2);
        public TimeSpan ErrorRetryInterval { get; set; } = TimeSpan.FromSeconds(30);
    }
