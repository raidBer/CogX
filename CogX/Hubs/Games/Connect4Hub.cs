using Microsoft.AspNetCore.SignalR;
using CogX.Services.Games;
using CogX.Services;
using CogX.Models.Games;
using CogX.DTOs.Games;
using CogX.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CogX.Hubs.Games
{
    public class Connect4Hub : Hub
    {
        private readonly IConnect4Service _gameService;
        private readonly IGameHistoryService _historyService;
        private readonly CogXDbContext _context;
        private readonly ILogger<Connect4Hub> _logger;

        private static readonly ConcurrentDictionary<Guid, Connect4State> _activeGames = new();

        public Connect4Hub(
            IConnect4Service gameService,
            IGameHistoryService historyService,
            CogXDbContext context,
            ILogger<Connect4Hub> logger)
        {
            _gameService = gameService;
            _historyService = historyService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Initialiser une partie de Puissance 4
        /// </summary>
        public async Task InitializeGame(string lobbyId, Guid gameSessionId, List<Guid> playerIds)
        {
            try
            {
                if (playerIds.Count != 2)
                {
                    await Clients.Group(lobbyId).SendAsync("GameError", "Connect 4 requires exactly 2 players");
                    return;
                }

                var players = await _context.Players
                    .Where(p => playerIds.Contains(p.Id))
                    .ToListAsync();

                if (players.Count != 2)
                {
                    await Clients.Group(lobbyId).SendAsync("GameError", "Players not found");
                    return;
                }

                var player1 = players[0];
                var player2 = players[1];

                var gameState = _gameService.InitializeGame(gameSessionId, player1.Id, player2.Id);
                _activeGames[gameSessionId] = gameState;

                await _historyService.LogAction(
                    gameSessionId,
                    player1.Id,
                    "GameInitialized",
                    new { GameType = "Connect4", Player1 = player1.Pseudo, Player2 = player2.Pseudo }
                );

                var gameDto = MapToDto(gameState, players);
                await Clients.Group(lobbyId).SendAsync("Connect4Initialized", gameDto);

                _logger.LogInformation("Connect4 game initialized for session {GameSessionId}", gameSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Connect4 game");
                await Clients.Group(lobbyId).SendAsync("GameError", "Failed to initialize game");
            }
        }

        /// <summary>
        /// Placer un pion dans une colonne
        /// </summary>
        public async Task DropPiece(string lobbyId, Guid gameSessionId, Guid playerId, int column)
        {
            try
            {
                if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                {
                    await Clients.Caller.SendAsync("GameError", "Game not found");
                    return;
                }

                if (!_gameService.IsValidMove(gameState, playerId, column))
                {
                    await Clients.Caller.SendAsync("InvalidMove", "This move is not valid");
                    return;
                }

                var playerNumber = _gameService.GetPlayerNumber(gameState, playerId);
                var (updatedState, row) = _gameService.DropPiece(gameState, playerId, column);
                _activeGames[gameSessionId] = updatedState;

                await _historyService.LogAction(
                    gameSessionId,
                    playerId,
                    "DropPiece",
                    new { Row = row, Column = column, PlayerNumber = playerNumber }
                );

                var players = await _context.Players
                    .Where(p => p.Id == updatedState.Player1Id || p.Id == updatedState.Player2Id)
                    .ToListAsync();

                var gameDto = MapToDto(updatedState, players);

                // Notifier tous les joueurs (avec animation de chute)
                await Clients.Group(lobbyId).SendAsync("PieceDropped", new
                {
                    Row = row,
                    Column = column,
                    PlayerNumber = playerNumber,
                    PlayerId = playerId,
                    PlayerColor = playerId == updatedState.Player1Id ? updatedState.Player1Color : updatedState.Player2Color,
                    GameState = gameDto
                });

                // Si le jeu est terminé
                if (updatedState.IsGameOver)
                {
                    string result;
                    if (updatedState.IsDraw)
                    {
                        result = "Draw";
                        await _historyService.LogAction(gameSessionId, playerId, "GameEnded", new { Result = "Draw" });
                    }
                    else
                    {
                        result = "Win";
                        var winner = players.FirstOrDefault(p => p.Id == updatedState.WinnerId);
                        await _historyService.LogAction(
                            gameSessionId,
                            updatedState.WinnerId!.Value,
                            "GameEnded",
                            new { Result = "Win", Winner = winner?.Pseudo, WinningLine = updatedState.WinningLine }
                        );
                    }

                    var session = await _context.GameSessions.FindAsync(gameSessionId);
                    if (session != null)
                    {
                        session.FinishedAt = DateTime.UtcNow;
                        session.GameState = JsonSerializer.Serialize(updatedState);
                        await _context.SaveChangesAsync();
                    }

                    await Clients.Group(lobbyId).SendAsync("GameOver", new
                    {
                        Result = result,
                        WinnerId = updatedState.WinnerId,
                        WinnerPseudo = players.FirstOrDefault(p => p.Id == updatedState.WinnerId)?.Pseudo,
                        IsDraw = updatedState.IsDraw,
                        WinningLine = updatedState.WinningLine,
                        TotalMoves = updatedState.TotalMoves,
                        GameState = gameDto
                    });

                    _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ =>
                    {
                        _activeGames.TryRemove(gameSessionId, out var _);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dropping piece");
                await Clients.Caller.SendAsync("GameError", "Failed to drop piece");
            }
        }

        /// <summary>
        /// Rejoindre la room
        /// </summary>
        public async Task JoinGameRoom(string lobbyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            _logger.LogInformation("Client joined Connect4 game {LobbyId}", lobbyId);
        }

        /// <summary>
        /// Quitter la room
        /// </summary>
        public async Task LeaveGameRoom(string lobbyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
        }

        /// <summary>
        /// Récupérer l'état du jeu
        /// </summary>
        public async Task<Connect4GameDto?> GetGameState(Guid gameSessionId)
        {
            if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                return null;

            var players = await _context.Players
                .Where(p => p.Id == gameState.Player1Id || p.Id == gameState.Player2Id)
                .ToListAsync();

            return MapToDto(gameState, players);
        }

        /// <summary>
        /// Abandonner
        /// </summary>
        public async Task Forfeit(string lobbyId, Guid gameSessionId, Guid playerId)
        {
            try
            {
                if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                {
                    await Clients.Caller.SendAsync("GameError", "Game not found");
                    return;
                }

                var winnerId = playerId == gameState.Player1Id ? gameState.Player2Id : gameState.Player1Id;
                var players = await _context.Players
                    .Where(p => p.Id == gameState.Player1Id || p.Id == gameState.Player2Id)
                    .ToListAsync();

                var winner = players.FirstOrDefault(p => p.Id == winnerId);
                var forfeiter = players.FirstOrDefault(p => p.Id == playerId);

                gameState.WinnerId = winnerId;
                gameState.IsGameOver = true;

                await _historyService.LogAction(
                    gameSessionId,
                    playerId,
                    "PlayerForfeited",
                    new { Forfeiter = forfeiter?.Pseudo, Winner = winner?.Pseudo }
                );

                var session = await _context.GameSessions.FindAsync(gameSessionId);
                if (session != null)
                {
                    session.FinishedAt = DateTime.UtcNow;
                    session.GameState = JsonSerializer.Serialize(gameState);
                    await _context.SaveChangesAsync();
                }

                await Clients.Group(lobbyId).SendAsync("PlayerForfeited", new
                {
                    ForfeiterPseudo = forfeiter?.Pseudo,
                    WinnerPseudo = winner?.Pseudo,
                    WinnerId = winnerId
                });

                _activeGames.TryRemove(gameSessionId, out var _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forfeit");
                await Clients.Caller.SendAsync("GameError", "Failed to process forfeit");
            }
        }

        private static Connect4GameDto MapToDto(Connect4State state, List<Models.Player> players)
        {
            var player1 = players.FirstOrDefault(p => p.Id == state.Player1Id);
            var player2 = players.FirstOrDefault(p => p.Id == state.Player2Id);

            return new Connect4GameDto
            {
                GameSessionId = state.GameSessionId,
                Board = state.Board,
                Player1 = new PlayerGameInfoDto
                {
                    Id = state.Player1Id,
                    Pseudo = player1?.Pseudo ?? "Player 1",
                    Symbol = state.Player1Color
                },
                Player2 = new PlayerGameInfoDto
                {
                    Id = state.Player2Id,
                    Pseudo = player2?.Pseudo ?? "Player 2",
                    Symbol = state.Player2Color
                },
                CurrentPlayerTurn = state.CurrentPlayerTurn,
                WinnerId = state.WinnerId?.ToString(),
                IsDraw = state.IsDraw,
                IsGameOver = state.IsGameOver,
                WinningLine = state.WinningLine?.Select(w => new WinningPositionDto
                {
                    Row = w.Row,
                    Col = w.Col
                }).ToList(),
                TotalMoves = state.TotalMoves
            };
        }
    }
}
