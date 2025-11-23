using CogX.Models.Games;

namespace CogX.Services.Games
{
    public interface ITicTacToeService
    {
        TicTacToeState InitializeGame(Guid gameSessionId, Guid player1Id, Guid player2Id);
        bool IsValidMove(TicTacToeState state, Guid playerId, int row, int col);
        TicTacToeState MakeMove(TicTacToeState state, Guid playerId, int row, int col);
        bool CheckWin(string[,] board, string symbol, out List<WinningPosition>? winningLine);
        bool CheckDraw(string[,] board);
        string GetPlayerSymbol(TicTacToeState state, Guid playerId);
    }

    public class TicTacToeService : ITicTacToeService
    {
        public TicTacToeState InitializeGame(Guid gameSessionId, Guid player1Id, Guid player2Id)
        {
            return new TicTacToeState
            {
                GameSessionId = gameSessionId,
                Player1Id = player1Id,
                Player2Id = player2Id,
                CurrentPlayerTurn = player1Id, // Player1 commence
                Board = new string[3, 3],
                IsGameOver = false,
                LastMoveTime = DateTime.UtcNow
            };
        }

        public bool IsValidMove(TicTacToeState state, Guid playerId, int row, int col)
        {
            // Vérifier que le jeu n'est pas terminé
            if (state.IsGameOver)
                return false;

            // Vérifier que c'est le tour du joueur
            if (state.CurrentPlayerTurn != playerId)
                return false;

            // Vérifier que la case est dans les limites
            if (row < 0 || row > 2 || col < 0 || col > 2)
                return false;

            // Vérifier que la case est vide
            if (!string.IsNullOrEmpty(state.Board[row, col]))
                return false;

            return true;
        }

        public TicTacToeState MakeMove(TicTacToeState state, Guid playerId, int row, int col)
        {
            if (!IsValidMove(state, playerId, row, col))
                throw new InvalidOperationException("Invalid move");

            var symbol = GetPlayerSymbol(state, playerId);
            state.Board[row, col] = symbol;
            state.LastMoveTime = DateTime.UtcNow;

            // Vérifier victoire
            if (CheckWin(state.Board, symbol, out var winningLine))
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

            return state;
        }

        public bool CheckWin(string[,] board, string symbol, out List<WinningPosition>? winningLine)
        {
            winningLine = null;

            // Vérifier les lignes
            for (int i = 0; i < 3; i++)
            {
                if (board[i, 0] == symbol && board[i, 1] == symbol && board[i, 2] == symbol)
                {
                    winningLine = new List<WinningPosition>
                    {
                        new() { Row = i, Col = 0 },
                        new() { Row = i, Col = 1 },
                        new() { Row = i, Col = 2 }
                    };
                    return true;
                }
            }

            // Vérifier les colonnes
            for (int i = 0; i < 3; i++)
            {
                if (board[0, i] == symbol && board[1, i] == symbol && board[2, i] == symbol)
                {
                    winningLine = new List<WinningPosition>
                    {
                        new() { Row = 0, Col = i },
                        new() { Row = 1, Col = i },
                        new() { Row = 2, Col = i }
                    };
                    return true;
                }
            }

            // Vérifier diagonale principale (haut-gauche à bas-droite)
            if (board[0, 0] == symbol && board[1, 1] == symbol && board[2, 2] == symbol)
            {
                winningLine = new List<WinningPosition>
                {
                    new() { Row = 0, Col = 0 },
                    new() { Row = 1, Col = 1 },
                    new() { Row = 2, Col = 2 }
                };
                return true;
            }

            // Vérifier diagonale secondaire (haut-droite à bas-gauche)
            if (board[0, 2] == symbol && board[1, 1] == symbol && board[2, 0] == symbol)
            {
                winningLine = new List<WinningPosition>
                {
                    new() { Row = 0, Col = 2 },
                    new() { Row = 1, Col = 1 },
                    new() { Row = 2, Col = 0 }
                };
                return true;
            }

            return false;
        }

        public bool CheckDraw(string[,] board)
        {
            // Match nul si toutes les cases sont remplies
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (string.IsNullOrEmpty(board[i, j]))
                        return false;
                }
            }
            return true;
        }

        public string GetPlayerSymbol(TicTacToeState state, Guid playerId)
        {
            return playerId == state.Player1Id ? state.Player1Symbol : state.Player2Symbol;
        }
    }
}
