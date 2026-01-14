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
    public class TicTacToeHub : Hub
    {
        private readonly ITicTacToeService _gameService;
        private readonly IGameHistoryService _historyService;
        private readonly CogXDbContext _context;
        private readonly ILogger<TicTacToeHub> _logger;

        // Stockage en mémoire des parties actives (en production : utiliser Redis)
        private static readonly ConcurrentDictionary<Guid, TicTacToeState> _activeGames = new();

        public TicTacToeHub(
            ITicTacToeService gameService,
            IGameHistoryService historyService,
            CogXDbContext context,
            ILogger<TicTacToeHub> logger)
        {
            _gameService = gameService;
            _historyService = historyService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Initialiser une nouvelle partie de Morpion
        /// </summary>
        public async Task InitializeGame(string lobbyId, Guid gameSessionId, List<Guid> playerIds)
        {
            try
            {
                if (playerIds.Count != 2)
                {
                    await Clients.Group(lobbyId).SendAsync("GameError", "Morpion requires exactly 2 players");
                    return;
                }

                // Charger les informations des joueurs
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

                // Initialiser l'état du jeu
                var gameState = _gameService.InitializeGame(gameSessionId, player1.Id, player2.Id);
                _activeGames[gameSessionId] = gameState;

                // Logger l'initialisation
                await _historyService.LogAction(
                    gameSessionId,
                    player1.Id,
                    "GameInitialized",
                    new { GameType = "Morpion", Player1 = player1.Pseudo, Player2 = player2.Pseudo }
                );

                // Envoyer l'état initial à tous les joueurs
                var gameDto = MapToDto(gameState, players);
                await Clients.Group(lobbyId).SendAsync("TicTacToeInitialized", gameDto);

                _logger.LogInformation("TicTacToe game initialized for session {GameSessionId}", gameSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing TicTacToe game");
                await Clients.Group(lobbyId).SendAsync("GameError", "Failed to initialize game");
            }
        }

        /// <summary>
        /// Jouer un coup
        /// </summary>
        public async Task MakeMove(string lobbyId, Guid gameSessionId, Guid playerId, int row, int col)
        {
            try
            {
                if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                {
                    await Clients.Caller.SendAsync("GameError", "Game not found");
                    return;
                }

                // Valider et effectuer le coup
                if (!_gameService.IsValidMove(gameState, playerId, row, col))
                {
                    await Clients.Caller.SendAsync("InvalidMove", "This move is not valid");
                    return;
                }

                var symbol = _gameService.GetPlayerSymbol(gameState, playerId);
                gameState = _gameService.MakeMove(gameState, playerId, row, col);
                _activeGames[gameSessionId] = gameState;

                // Logger l'action
                await _historyService.LogAction(
                    gameSessionId,
                    playerId,
                    "PlacePiece",
                    new { Row = row, Col = col, Symbol = symbol }
                );

                // Charger les infos des joueurs
                var players = await _context.Players
                    .Where(p => p.Id == gameState.Player1Id || p.Id == gameState.Player2Id)
                    .ToListAsync();

                var gameDto = MapToDto(gameState, players);

                // Notifier tous les joueurs du coup
                await Clients.Group(lobbyId).SendAsync("MoveMade", new
                {
                    Row = row,
                    Col = col,
                    Symbol = symbol,
                    PlayerId = playerId,
                    GameState = gameDto
                });

                // Si le jeu est terminé
                if (gameState.IsGameOver)
                {
                    string result;
                    if (gameState.IsDraw)
                    {
                        result = "Draw";
                        await _historyService.LogAction(gameSessionId, playerId, "GameEnded", new { Result = "Draw" });
                    }
                    else
                    {
                        result = "Win";
                        var winner = players.FirstOrDefault(p => p.Id == gameState.WinnerId);
                        await _historyService.LogAction(
                            gameSessionId,
                            gameState.WinnerId!.Value,
                            "GameEnded",
                            new { Result = "Win", Winner = winner?.Pseudo }
                        );
                    }

                    // Mettre à jour la session dans la BDD
                    var session = await _context.GameSessions.FindAsync(gameSessionId);
                    if (session != null)
                    {
                        session.FinishedAt = DateTime.UtcNow;
                        // Serialize the DTO instead of gameState to avoid multidimensional array issue
                        session.GameState = JsonSerializer.Serialize(gameDto);
                        await _context.SaveChangesAsync();
                    }

                    await Clients.Group(lobbyId).SendAsync("GameOver", new
                    {
                        Result = result,
                        WinnerId = gameState.WinnerId,
                        WinnerPseudo = players.FirstOrDefault(p => p.Id == gameState.WinnerId)?.Pseudo,
                        IsDraw = gameState.IsDraw,
                        WinningLine = gameState.WinningLine,
                        GameState = gameDto
                    });

                    // Retirer le jeu de la mémoire après 1 minute
                    _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ =>
                    {
                        _activeGames.TryRemove(gameSessionId, out var _);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making move in TicTacToe");
                await Clients.Caller.SendAsync("GameError", "Failed to make move");
            }
        }

        /// <summary>
        /// Rejoindre la room du jeu
        /// </summary>
        public async Task JoinGameRoom(string lobbyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            _logger.LogInformation("Client {ConnectionId} joined TicTacToe game {LobbyId}", 
                Context.ConnectionId, lobbyId);
        }

        /// <summary>
        /// Quitter la room du jeu
        /// </summary>
        public async Task LeaveGameRoom(string lobbyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
            _logger.LogInformation("Client {ConnectionId} left TicTacToe game {LobbyId}", 
                Context.ConnectionId, lobbyId);
        }

        /// <summary>
        /// Récupérer l'état actuel du jeu
        /// </summary>
        public async Task<TicTacToeGameDto?> GetGameState(Guid gameSessionId)
        {
            if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                return null;

            var players = await _context.Players
                .Where(p => p.Id == gameState.Player1Id || p.Id == gameState.Player2Id)
                .ToListAsync();

            return MapToDto(gameState, players);
        }

        /// <summary>
        /// Abandonner la partie
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

                // Create DTO for serialization
                var gameDto = MapToDto(gameState, players);

                var session = await _context.GameSessions.FindAsync(gameSessionId);
                if (session != null)
                {
                    session.FinishedAt = DateTime.UtcNow;
                    // Serialize the DTO instead of gameState to avoid multidimensional array issue
                    session.GameState = JsonSerializer.Serialize(gameDto);
                    await _context.SaveChangesAsync();
                }

                await Clients.Group(lobbyId).SendAsync("PlayerForfeited", new
                {
                    ForfeiterPseudo = forfeiter?.Pseudo,
                    WinnerPseudo = winner?.Pseudo,
                    WinnerId = winnerId
                });

                _activeGames.TryRemove(gameSessionId, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forfeit");
                await Clients.Caller.SendAsync("GameError", "Failed to process forfeit");
            }
        }

        private static TicTacToeGameDto MapToDto(TicTacToeState state, List<Models.Player> players)
        {
            var player1 = players.FirstOrDefault(p => p.Id == state.Player1Id);
            var player2 = players.FirstOrDefault(p => p.Id == state.Player2Id);

            // Convert multidimensional array to jagged array for JSON serialization
            var jaggedBoard = new string[3][];
            for (int i = 0; i < 3; i++)
            {
                jaggedBoard[i] = new string[3];
                for (int j = 0; j < 3; j++)
                {
                    jaggedBoard[i][j] = state.Board[i, j];
                }
            }

            return new TicTacToeGameDto
            {
                GameSessionId = state.GameSessionId,
                Board = jaggedBoard,
                Player1 = new PlayerGameInfoDto
                {
                    Id = state.Player1Id,
                    Pseudo = player1?.Pseudo ?? "Player 1",
                    Symbol = state.Player1Symbol
                },
                Player2 = new PlayerGameInfoDto
                {
                    Id = state.Player2Id,
                    Pseudo = player2?.Pseudo ?? "Player 2",
                    Symbol = state.Player2Symbol
                },
                CurrentPlayerTurn = state.CurrentPlayerTurn,
                WinnerId = state.WinnerId?.ToString(),
                IsDraw = state.IsDraw,
                IsGameOver = state.IsGameOver,
                WinningLine = state.WinningLine?.Select(w => new WinningPositionDto
                {
                    Row = w.Row,
                    Col = w.Col
                }).ToList()
            };
        }
    }
}
