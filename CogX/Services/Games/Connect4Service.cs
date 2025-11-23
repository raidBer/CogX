using CogX.Models.Games;

namespace CogX.Services.Games
{
    public interface IConnect4Service
    {
        Connect4State InitializeGame(Guid gameSessionId, Guid player1Id, Guid player2Id);
        bool IsValidMove(Connect4State state, Guid playerId, int column);
        (Connect4State state, int row) DropPiece(Connect4State state, Guid playerId, int column);
        bool CheckWin(int[,] board, int playerNumber, int lastRow, int lastCol, out List<WinningPosition>? winningLine);
        bool CheckDraw(int[,] board);
        int GetPlayerNumber(Connect4State state, Guid playerId);
    }

    public class Connect4Service : IConnect4Service
    {
        private const int Rows = 6;
        private const int Cols = 7;

        public Connect4State InitializeGame(Guid gameSessionId, Guid player1Id, Guid player2Id)
        {
            return new Connect4State
            {
                GameSessionId = gameSessionId,
                Player1Id = player1Id,
                Player2Id = player2Id,
                CurrentPlayerTurn = player1Id,
                Board = new int[Rows, Cols],
                IsGameOver = false,
                LastMoveTime = DateTime.UtcNow,
                TotalMoves = 0
            };
        }

        public bool IsValidMove(Connect4State state, Guid playerId, int column)
        {
            if (state.IsGameOver)
                return false;

            if (state.CurrentPlayerTurn != playerId)
                return false;

            if (column < 0 || column >= Cols)
                return false;

            // Vérifier si la colonne n'est pas pleine
            return state.Board[0, column] == 0;
        }

        public (Connect4State state, int row) DropPiece(Connect4State state, Guid playerId, int column)
        {
            if (!IsValidMove(state, playerId, column))
                throw new InvalidOperationException("Invalid move");

            var playerNumber = GetPlayerNumber(state, playerId);

            // Trouver la ligne la plus basse disponible dans la colonne
            int row = -1;
            for (int r = Rows - 1; r >= 0; r--)
            {
                if (state.Board[r, column] == 0)
                {
                    row = r;
                    break;
                }
            }

            if (row == -1)
                throw new InvalidOperationException("Column is full");

            state.Board[row, column] = playerNumber;
            state.TotalMoves++;
            state.LastMoveTime = DateTime.UtcNow;

            // Vérifier victoire
            if (CheckWin(state.Board, playerNumber, row, column, out var winningLine))
            {
                state.WinnerId = playerId;
                state.IsGameOver = true;
                state.WinningLine = winningLine;
            }
            // Vérifier match nul
            else if (CheckDraw(state.Board))
            {
                state.IsDraw = true;
                state.IsGameOver = true;
            }
            // Changer de tour
            else
            {
                state.CurrentPlayerTurn = playerId == state.Player1Id
                    ? state.Player2Id
                    : state.Player1Id;
            }

            return (state, row);
        }

        public bool CheckWin(int[,] board, int playerNumber, int lastRow, int lastCol, out List<WinningPosition>? winningLine)
        {
            winningLine = null;

            // Directions: horizontal, vertical, diagonale /, diagonale \
            int[][] directions = new int[][]
            {
                new int[] { 0, 1 },  // Horizontal
                new int[] { 1, 0 },  // Vertical
                new int[] { 1, 1 },  // Diagonale \
                new int[] { 1, -1 }  // Diagonale /
            };

            foreach (var dir in directions)
            {
                var positions = new List<WinningPosition>();
                int count = 1; // Le dernier pion placé

                positions.Add(new WinningPosition { Row = lastRow, Col = lastCol });

                // Vérifier dans une direction
                count += CountInDirection(board, playerNumber, lastRow, lastCol, dir[0], dir[1], positions);
                // Vérifier dans la direction opposée
                count += CountInDirection(board, playerNumber, lastRow, lastCol, -dir[0], -dir[1], positions);

                if (count >= 4)
                {
                    winningLine = positions;
                    return true;
                }
            }

            return false;
        }

        private int CountInDirection(int[,] board, int playerNumber, int row, int col, int dRow, int dCol, List<WinningPosition> positions)
        {
            int count = 0;
            int r = row + dRow;
            int c = col + dCol;

            while (r >= 0 && r < Rows && c >= 0 && c < Cols && board[r, c] == playerNumber)
            {
                positions.Add(new WinningPosition { Row = r, Col = c });
                count++;
                r += dRow;
                c += dCol;
            }

            return count;
        }

        public bool CheckDraw(int[,] board)
        {
            // Match nul si la première ligne est pleine
            for (int c = 0; c < Cols; c++)
            {
                if (board[0, c] == 0)
                    return false;
            }
            return true;
        }

        public int GetPlayerNumber(Connect4State state, Guid playerId)
        {
            return playerId == state.Player1Id ? state.Player1Number : state.Player2Number;
        }
    }
}
