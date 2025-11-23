namespace CogX.DTOs
{
    public class GameActionDto
    {
        public Guid Id { get; set; }
        public string PlayerPseudo { get; set; } = string.Empty;
        public Guid PlayerId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string ActionData { get; set; } = string.Empty;
        public object? ParsedActionData { get; set; } // JSON parsé
        public DateTime Timestamp { get; set; }
        public string TimeSinceStart { get; set; } = string.Empty; // "00:05" = 5 secondes
    }

    public class GameHistoryResponse
    {
        public Guid GameSessionId { get; set; }
        public string GameType { get; set; } = string.Empty;
        public DateTime GameStartedAt { get; set; }
        public DateTime? GameFinishedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public List<GameActionDto> Actions { get; set; } = new();
        public Dictionary<string, int> ActionStats { get; set; } = new();
        public List<PlayerSummaryDto> PlayerSummaries { get; set; } = new();
    }

    public class PlayerSummaryDto
    {
        public Guid PlayerId { get; set; }
        public string Pseudo { get; set; } = string.Empty;
        public int ActionCount { get; set; }
        public Dictionary<string, int> ActionBreakdown { get; set; } = new();
    }

    public class GameSessionSummaryDto
    {
        public Guid Id { get; set; }
        public string GameType { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int TotalActions { get; set; }
        public int PlayerCount { get; set; }
    }
}
