namespace CogX.DTOs
{
    public class LeaderboardDto
    {
        public int Rank { get; set; }
        public string Pseudo { get; set; } = string.Empty;
        public int Score { get; set; }
        public TimeSpan? Time { get; set; }
        public string? TimeFormatted { get; set; } // "1m 23s" ou "45.2s"
        public DateTime AchievedAt { get; set; }
        public bool IsCurrentPlayer { get; set; }
    }

    public class AddScoreRequest
    {
        public string GameType { get; set; } = string.Empty;
        public Guid PlayerId { get; set; }
        public int Score { get; set; }
        public TimeSpan? Time { get; set; }
    }

    public class LeaderboardResponse
    {
        public string GameType { get; set; } = string.Empty;
        public List<LeaderboardDto> Entries { get; set; } = new();
        public int TotalEntries { get; set; }
        public LeaderboardDto? CurrentPlayerEntry { get; set; }
    }
}
