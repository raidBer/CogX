namespace CogX.Models
{
    public class LeaderboardEntry
    {
        public Guid Id { get; set; }
        public string GameType { get; set; } = string.Empty;
        public Guid PlayerId { get; set; }
        public Player? Player { get; set; }
        public int Score { get; set; }
        public TimeSpan? Time { get; set; }
        public DateTime AchievedAt { get; set; }
    }
}
