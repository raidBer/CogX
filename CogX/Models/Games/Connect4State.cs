namespace CogX.Models.Games
{
    /// <summary>
    /// État d'une partie de Puissance 4
    /// </summary>
    public class Connect4State
    {
        public Guid GameSessionId { get; set; }
        public int[,] Board { get; set; } = new int[6, 7]; // 6 lignes x 7 colonnes, 0=vide, 1=joueur1, 2=joueur2
        public Guid Player1Id { get; set; }
        public Guid Player2Id { get; set; }
        public int Player1Number { get; set; } = 1;
        public int Player2Number { get; set; } = 2;
        public string Player1Color { get; set; } = "red";
        public string Player2Color { get; set; } = "yellow";
        public Guid CurrentPlayerTurn { get; set; }
        public Guid? WinnerId { get; set; }
        public bool IsDraw { get; set; }
        public bool IsGameOver { get; set; }
        public List<WinningPosition>? WinningLine { get; set; }
        public DateTime LastMoveTime { get; set; }
        public int TotalMoves { get; set; }
    }
}
