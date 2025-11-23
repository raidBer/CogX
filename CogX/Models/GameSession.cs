namespace CogX.Models
{
    public class GameSession
    {
        public Guid Id { get; set; }
        public Guid LobbyId { get; set; }
        public Lobby? Lobby { get; set; }
        public string GameState { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}
