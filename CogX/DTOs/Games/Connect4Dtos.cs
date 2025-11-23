namespace CogX.DTOs.Games
{
    public class Connect4GameDto
    {
        public Guid GameSessionId { get; set; }
        public int[,] Board { get; set; } = new int[6, 7];
        public PlayerGameInfoDto Player1 { get; set; } = new();
        public PlayerGameInfoDto Player2 { get; set; } = new();
        public Guid CurrentPlayerTurn { get; set; }
        public string? WinnerId { get; set; }
        public bool IsDraw { get; set; }
        public bool IsGameOver { get; set; }
        public List<WinningPositionDto>? WinningLine { get; set; }
        public int TotalMoves { get; set; }
    }

    public class Connect4MoveRequest
    {
        public Guid PlayerId { get; set; }
        public int Column { get; set; }
    }

    public class Connect4MoveResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public Connect4GameDto? GameState { get; set; }
    }
}
