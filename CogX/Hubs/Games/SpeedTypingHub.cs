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
    public class SpeedTypingHub : Hub
    {
        private readonly ISpeedTypingService _gameService;
        private readonly IGameHistoryService _historyService;
        private readonly CogXDbContext _context;
        private readonly ILogger<SpeedTypingHub> _logger;

        private static readonly ConcurrentDictionary<Guid, SpeedTypingState> _activeGames = new();
        private static readonly ConcurrentDictionary<Guid, System.Threading.Timer> _gameTimers = new();

        public SpeedTypingHub(
            ISpeedTypingService gameService,
            IGameHistoryService historyService,
            CogXDbContext context,
            ILogger<SpeedTypingHub> logger)
        {
            _gameService = gameService;
            _historyService = historyService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Initialiser une partie de Speed Typing
        /// </summary>
        public async Task InitializeGame(string lobbyId, Guid gameSessionId, List<Guid> playerIds)
        {
            try
            {
                if (playerIds.Count < 2)
                {
                    await Clients.Group(lobbyId).SendAsync("GameError", "Speed Typing requires at least 2 players");
                    return;
                }

                var players = await _context.Players
                    .Where(p => playerIds.Contains(p.Id))
                    .ToListAsync();

                var playerInfos = players.Select(p => (p.Id, p.Pseudo)).ToList();
                var gameState = _gameService.InitializeGame(gameSessionId, playerInfos);
                _activeGames[gameSessionId] = gameState;

                await _historyService.LogAction(
                    gameSessionId,
                    players[0].Id,
                    "GameInitialized",
                    new { GameType = "SpeedTyping", PlayerCount = players.Count }
                );

                var gameDto = MapToDto(gameState);
                await Clients.Group(lobbyId).SendAsync("SpeedTypingInitialized", gameDto);

                _logger.LogInformation("SpeedTyping game initialized for session {GameSessionId}", gameSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing SpeedTyping game");
                await Clients.Group(lobbyId).SendAsync("GameError", "Failed to initialize game");
            }
        }

        /// <summary>
        /// Démarrer la course (countdown de 3 secondes puis start)
        /// </summary>
        public async Task StartRace(string lobbyId, Guid gameSessionId)
        {
            try
            {
                if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                {
                    await Clients.Caller.SendAsync("GameError", "Game not found");
                    return;
                }

                if (gameState.IsStarted)
                {
                    await Clients.Caller.SendAsync("GameError", "Race already started");
                    return;
                }

                // Countdown
                for (int i = 3; i > 0; i--)
                {
                    await Clients.Group(lobbyId).SendAsync("Countdown", i);
                    await Task.Delay(1000);
                }

                gameState.IsStarted = true;
                gameState.StartTime = DateTime.UtcNow;
                _activeGames[gameSessionId] = gameState;

                await Clients.Group(lobbyId).SendAsync("RaceStarted", new
                {
                    GameSessionId = gameSessionId,
                    StartTime = gameState.StartTime,
                    TextToType = gameState.TextToType
                });

                await _historyService.LogAction(
                    gameSessionId,
                    Guid.Empty,
                    "RaceStarted",
                    new { StartTime = gameState.StartTime }
                );

                // Timer pour terminer automatiquement après maxDuration
                var timer = new System.Threading.Timer(async _ =>
                {
                    await TimeoutRace(lobbyId, gameSessionId);
                }, null, TimeSpan.FromSeconds(gameState.MaxDurationSeconds), Timeout.InfiniteTimeSpan);

                _gameTimers[gameSessionId] = timer;

                _logger.LogInformation("SpeedTyping race started for session {GameSessionId}", gameSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting race");
                await Clients.Caller.SendAsync("GameError", "Failed to start race");
            }
        }

        /// <summary>
        /// Mettre à jour la progression d'un joueur
        /// </summary>
        public async Task UpdateProgress(string lobbyId, Guid gameSessionId, Guid playerId, int charactersTyped, int errorCount)
        {
            try
            {
                _logger.LogInformation("UpdateProgress received: SessionId={SessionId}, PlayerId={PlayerId}, Chars={Chars}, Errors={Errors}", 
                    gameSessionId, playerId, charactersTyped, errorCount);

                if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                {
                    _logger.LogWarning("Game not found for session {SessionId}", gameSessionId);
                    await Clients.Caller.SendAsync("GameError", "Game not found");
                    return;
                }

                if (!gameState.IsStarted)
                {
                    _logger.LogWarning("Race not started yet for session {SessionId}", gameSessionId);
                    await Clients.Caller.SendAsync("GameError", "Race not started yet");
                    return;
                }

                if (gameState.IsFinished)
                {
                    return; // Ignorer les updates après la fin
                }

                var previousProgress = gameState.PlayerProgressMap[playerId].CharactersTyped;
                gameState = _gameService.UpdatePlayerProgress(gameState, playerId, charactersTyped, errorCount);
                _activeGames[gameSessionId] = gameState;

                var playerProgress = gameState.PlayerProgressMap[playerId];

                // Notifier tous les joueurs de la mise à jour
                await Clients.Group(lobbyId).SendAsync("ProgressUpdated", new
                {
                    PlayerId = playerId,
                    Pseudo = playerProgress.Pseudo,
                    CharactersTyped = playerProgress.CharactersTyped,
                    ProgressPercentage = playerProgress.ProgressPercentage,
                    WPM = playerProgress.WPM,
                    Accuracy = playerProgress.Accuracy
                });

                // Logger toutes les 10 caractères
                if (charactersTyped % 10 == 0 && charactersTyped != previousProgress)
                {
                    await _historyService.LogAction(
                        gameSessionId,
                        playerId,
                        "ProgressUpdate",
                        new
                        {
                            CharactersTyped = charactersTyped,
                            WPM = playerProgress.WPM,
                            Accuracy = playerProgress.Accuracy
                        }
                    );
                }

                // Si le joueur vient de finir
                if (playerProgress.HasFinished && previousProgress < gameState.TextToType.Length)
                {
                    _logger.LogInformation("Player {PlayerId} finished with rank {Rank}", playerId, playerProgress.Rank);
                    
                    await Clients.Group(lobbyId).SendAsync("PlayerFinished", new
                    {
                        PlayerId = playerId,
                        Pseudo = playerProgress.Pseudo,
                        FinishTime = playerProgress.FinishTime,
                        FinishTimeFormatted = FormatTime(playerProgress.FinishTime!.Value),
                        Rank = playerProgress.Rank,
                        WPM = playerProgress.WPM,
                        Accuracy = playerProgress.Accuracy
                    });

                    await _historyService.LogAction(
                        gameSessionId,
                        playerId,
                        "PlayerFinished",
                        new
                        {
                            Rank = playerProgress.Rank,
                            FinishTime = playerProgress.FinishTime,
                            WPM = playerProgress.WPM,
                            Accuracy = playerProgress.Accuracy
                        }
                    );
                }

                // Si tous les joueurs ont terminé
                if (gameState.IsFinished)
                {
                    await EndRace(lobbyId, gameSessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress");
                await Clients.Caller.SendAsync("GameError", "Failed to update progress");
            }
        }

        /// <summary>
        /// Terminer la course
        /// </summary>
        private async Task EndRace(string lobbyId, Guid gameSessionId)
        {
            if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                return;

            // Arrêter le timer
            if (_gameTimers.TryRemove(gameSessionId, out var timer))
            {
                timer.Dispose();
            }

            gameState.IsFinished = true;
            gameState.EndTime = DateTime.UtcNow;

            // Mettre à jour la BDD
            var session = await _context.GameSessions.FindAsync(gameSessionId);
            if (session != null)
            {
                session.FinishedAt = DateTime.UtcNow;
                session.GameState = JsonSerializer.Serialize(gameState);
                await _context.SaveChangesAsync();
            }

            // Créer le classement final
            var finalResults = gameState.PlayerProgressMap.Values
                .OrderBy(p => p.HasFinished ? p.Rank : int.MaxValue)
                .ThenByDescending(p => p.CharactersTyped)
                .Select(p => new PlayerProgressDto
                {
                    PlayerId = p.PlayerId,
                    Pseudo = p.Pseudo,
                    CharactersTyped = p.CharactersTyped,
                    TotalCharacters = p.TotalCharacters,
                    ProgressPercentage = p.ProgressPercentage,
                    WPM = p.WPM,
                    Accuracy = p.Accuracy,
                    FinishTime = p.FinishTime,
                    FinishTimeFormatted = p.FinishTime.HasValue ? FormatTime(p.FinishTime.Value) : null,
                    Rank = p.Rank,
                    HasFinished = p.HasFinished
                })
                .ToList();

            await Clients.Group(lobbyId).SendAsync("RaceEnded", new
            {
                GameSessionId = gameSessionId,
                FinalResults = finalResults,
                Duration = gameState.EndTime - gameState.StartTime
            });

            await _historyService.LogAction(
                gameSessionId,
                Guid.Empty,
                "RaceEnded",
                new { FinalResults = finalResults }
            );

            // Nettoyer après 2 minutes
            _ = Task.Delay(TimeSpan.FromMinutes(2)).ContinueWith(_ =>
            {
                _activeGames.TryRemove(gameSessionId, out var _);
            });

            _logger.LogInformation("SpeedTyping race ended for session {GameSessionId}", gameSessionId);
        }

        /// <summary>
        /// Timeout de la course (durée maximale atteinte)
        /// </summary>
        private async Task TimeoutRace(string lobbyId, Guid gameSessionId)
        {
            if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                return;

            if (gameState.IsFinished)
                return;

            await Clients.Group(lobbyId).SendAsync("RaceTimeout", "Time's up!");
            await EndRace(lobbyId, gameSessionId);
        }

        /// <summary>
        /// Rejoindre la room
        /// </summary>
        public async Task JoinGameRoom(string lobbyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            _logger.LogInformation("Client joined SpeedTyping game {LobbyId}", lobbyId);
        }

        /// <summary>
        /// Quitter la room
        /// </summary>
        public async Task LeaveGameRoom(string lobbyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
        }

        /// <summary>
        /// Récupérer l'état actuel
        /// </summary>
        public async Task<SpeedTypingGameDto?> GetGameState(Guid gameSessionId)
        {
            await Task.CompletedTask;
            
            if (!_activeGames.TryGetValue(gameSessionId, out var gameState))
                return null;

            return MapToDto(gameState);
        }

        private static SpeedTypingGameDto MapToDto(SpeedTypingState state)
        {
            var secondsRemaining = 0;
            if (state.IsStarted && !state.IsFinished)
            {
                var elapsed = DateTime.UtcNow - state.StartTime;
                secondsRemaining = Math.Max(0, state.MaxDurationSeconds - (int)elapsed.TotalSeconds);
            }

            return new SpeedTypingGameDto
            {
                GameSessionId = state.GameSessionId,
                TextToType = state.TextToType,
                Players = state.PlayerProgressMap.Values.Select(p => new PlayerProgressDto
                {
                    PlayerId = p.PlayerId,
                    Pseudo = p.Pseudo,
                    CharactersTyped = p.CharactersTyped,
                    TotalCharacters = p.TotalCharacters,
                    ProgressPercentage = p.ProgressPercentage,
                    WPM = p.WPM,
                    Accuracy = p.Accuracy,
                    FinishTime = p.FinishTime,
                    FinishTimeFormatted = p.FinishTime.HasValue ? FormatTime(p.FinishTime.Value) : null,
                    Rank = p.Rank,
                    HasFinished = p.HasFinished
                }).ToList(),
                IsStarted = state.IsStarted,
                IsFinished = state.IsFinished,
                StartTime = state.IsStarted ? state.StartTime : null,
                EndTime = state.EndTime,
                SecondsRemaining = secondsRemaining
            };
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalMinutes >= 1)
                return $"{(int)time.TotalMinutes}m {time.Seconds}s";
            return $"{time.TotalSeconds:F1}s";
        }
    }
}
