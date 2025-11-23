namespace CogX.DTOs.Games
{
    public class SpeedTypingGameDto
    {
        public Guid GameSessionId { get; set; }
        public string TextToType { get; set; } = string.Empty;
        public List<PlayerProgressDto> Players { get; set; } = new();
        public bool IsStarted { get; set; }
        public bool IsFinished { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int SecondsRemaining { get; set; }
    }

    public class PlayerProgressDto
    {
        public Guid PlayerId { get; set; }
        public string Pseudo { get; set; } = string.Empty;
        public int CharactersTyped { get; set; }
        public int TotalCharacters { get; set; }
        public double ProgressPercentage { get; set; }
        public int WPM { get; set; }
        public int Accuracy { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public string? FinishTimeFormatted { get; set; }
        public int Rank { get; set; }
        public bool HasFinished { get; set; }
    }

    public class UpdateProgressRequest
    {
        public Guid PlayerId { get; set; }
        public int CharactersTyped { get; set; }
        public int ErrorCount { get; set; }
    }
}
