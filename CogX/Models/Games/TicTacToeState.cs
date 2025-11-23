namespace CogX.Models.Games
{
    /// <summary>
    /// État d'une partie de Morpion
    /// </summary>
    public class TicTacToeState
    {
        public Guid GameSessionId { get; set; }
        public string[,] Board { get; set; } = new string[3, 3]; // "X", "O", ou null
        public Guid Player1Id { get; set; }
        public Guid Player2Id { get; set; }
        public string Player1Symbol { get; set; } = "X";
        public string Player2Symbol { get; set; } = "O";
        public Guid CurrentPlayerTurn { get; set; }
        public Guid? WinnerId { get; set; }
        public bool IsDraw { get; set; }
        public bool IsGameOver { get; set; }
        public List<WinningPosition>? WinningLine { get; set; }
        public DateTime LastMoveTime { get; set; }
    }

    public class WinningPosition
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }
}
