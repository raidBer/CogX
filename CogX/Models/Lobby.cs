namespace CogX.Models
{
    public enum LobbyStatus
    {
        Waiting,
        InProgress,
        Finished
    }

    public class Lobby
    {
        public Guid Id { get; set; }
        public string GameType { get; set; } = string.Empty;
        public Guid HostId { get; set; }
        public Player? Host { get; set; }
        public string? Password { get; set; }
        public LobbyStatus Status { get; set; }
        public int MaxPlayers { get; set; }
        public List<Player> Players { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
