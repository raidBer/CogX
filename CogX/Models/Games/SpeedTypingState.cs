namespace CogX.Models.Games
{
    /// <summary>
    /// État d'une partie de Speed Typing
    /// </summary>
    public class SpeedTypingState
    {
        public Guid GameSessionId { get; set; }
        public string TextToType { get; set; } = string.Empty;
        public Dictionary<Guid, PlayerProgress> PlayerProgressMap { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsStarted { get; set; }
        public bool IsFinished { get; set; }
        public List<Guid> FinishedPlayerIds { get; set; } = new();
        public int MaxDurationSeconds { get; set; } = 180; // 3 minutes max
    }

    public class PlayerProgress
    {
        public Guid PlayerId { get; set; }
        public string Pseudo { get; set; } = string.Empty;
        public int CharactersTyped { get; set; }
        public int TotalCharacters { get; set; }
        public double ProgressPercentage { get; set; }
        public int WPM { get; set; } // Words Per Minute
        public int Accuracy { get; set; } // Pourcentage de précision
        public TimeSpan? FinishTime { get; set; }
        public int Rank { get; set; }
        public bool HasFinished { get; set; }
    }
}
