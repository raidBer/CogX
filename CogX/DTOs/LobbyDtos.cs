namespace CogX.DTOs
{
    public class CreatePlayerRequest
    {
        public string Pseudo { get; set; } = string.Empty;
    }

    public class CreateLobbyRequest
    {
        public Guid PlayerId { get; set; }
        public string GameType { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public string? Password { get; set; }
    }

    public class CreateLobbyResponse
    {
        public Guid LobbyId { get; set; }
        public string ShareLink { get; set; } = string.Empty;
    }

    public class JoinLobbyRequest
    {
        public Guid PlayerId { get; set; }
        public string? Password { get; set; }
    }

    public class LobbyDto
    {
        public Guid Id { get; set; }
        public string GameType { get; set; } = string.Empty;
        public string HostPseudo { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsPrivate { get; set; }
    }

    public class LobbyDetailsDto
    {
        public Guid Id { get; set; }
        public string GameType { get; set; } = string.Empty;
        public List<PlayerDto> Players { get; set; } = new();
        public int MaxPlayers { get; set; }
        public bool IsHost { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PlayerDto
    {
        public Guid Id { get; set; }
        public string Pseudo { get; set; } = string.Empty;
    }
}
