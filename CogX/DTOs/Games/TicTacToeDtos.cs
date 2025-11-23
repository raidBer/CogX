namespace CogX.DTOs.Games
{
    public class TicTacToeGameDto
    {
        public Guid GameSessionId { get; set; }
        public string[,] Board { get; set; } = new string[3, 3];
        public PlayerGameInfoDto Player1 { get; set; } = new();
        public PlayerGameInfoDto Player2 { get; set; } = new();
        public Guid CurrentPlayerTurn { get; set; }
        public string? WinnerId { get; set; }
        public bool IsDraw { get; set; }
        public bool IsGameOver { get; set; }
        public List<WinningPositionDto>? WinningLine { get; set; }
    }

    public class PlayerGameInfoDto
    {
        public Guid Id { get; set; }
        public string Pseudo { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
    }

    public class WinningPositionDto
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }

    public class MakeMoveRequest
    {
        public Guid PlayerId { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
    }

    public class MakeMoveResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TicTacToeGameDto? GameState { get; set; }
    }
}
