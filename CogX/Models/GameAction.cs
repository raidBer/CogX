namespace CogX.Models
{
    public class GameAction
    {
        public Guid Id { get; set; }
        public Guid GameSessionId { get; set; }
        public GameSession? GameSession { get; set; }
        public Guid PlayerId { get; set; }
        public Player? Player { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string ActionData { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
