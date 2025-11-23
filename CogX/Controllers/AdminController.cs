using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CogX.Data;
using CogX.Services;
using CogX.DTOs;
using System.Text.Json;

namespace CogX.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly CogXDbContext _context;
        private readonly IGameHistoryService _historyService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            CogXDbContext context,
            IGameHistoryService historyService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _historyService = historyService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère l'historique complet d'une session de jeu (pour contestations)
        /// </summary>
        [HttpGet("game-history/{gameSessionId}")]
        public async Task<ActionResult<GameHistoryResponse>> GetGameHistory(Guid gameSessionId)
        {
            var gameSession = await _context.GameSessions
                .Include(gs => gs.Lobby)
                .ThenInclude(l => l!.Players)
                .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

            if (gameSession == null)
            {
                return NotFound("Game session not found");
            }

            var actions = await _historyService.GetGameHistory(gameSessionId);
            var actionStats = await _historyService.GetActionStats(gameSessionId);

            var startTime = gameSession.StartedAt;
            var actionDtos = actions.Select(a => new GameActionDto
            {
                Id = a.Id,
                PlayerPseudo = a.Player?.Pseudo ?? "Unknown",
                PlayerId = a.PlayerId,
                ActionType = a.ActionType,
                ActionData = a.ActionData,
                ParsedActionData = TryParseJson(a.ActionData),
                Timestamp = a.Timestamp,
                TimeSinceStart = FormatTimeSinceStart(startTime, a.Timestamp)
            }).ToList();

            // Résumé par joueur
            var playerSummaries = actions
                .GroupBy(a => a.PlayerId)
                .Select(g => new PlayerSummaryDto
                {
                    PlayerId = g.Key,
                    Pseudo = g.First().Player?.Pseudo ?? "Unknown",
                    ActionCount = g.Count(),
                    ActionBreakdown = g.GroupBy(a => a.ActionType)
                                      .ToDictionary(ag => ag.Key, ag => ag.Count())
                })
                .ToList();

            var duration = (gameSession.FinishedAt ?? DateTime.UtcNow) - gameSession.StartedAt;

            return Ok(new GameHistoryResponse
            {
                GameSessionId = gameSessionId,
                GameType = gameSession.Lobby?.GameType ?? "Unknown",
                GameStartedAt = gameSession.StartedAt,
                GameFinishedAt = gameSession.FinishedAt,
                Duration = duration,
                Actions = actionDtos,
                ActionStats = actionStats,
                PlayerSummaries = playerSummaries
            });
        }

        /// <summary>
        /// Liste toutes les sessions de jeu avec statistiques
        /// </summary>
        [HttpGet("game-sessions")]
        public async Task<ActionResult<List<GameSessionSummaryDto>>> GetGameSessions(
            [FromQuery] string? gameType = null,
            [FromQuery] int limit = 50)
        {
            var query = _context.GameSessions
                .Include(gs => gs.Lobby)
                .ThenInclude(l => l!.Players)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(gameType))
            {
                query = query.Where(gs => gs.Lobby!.GameType == gameType);
            }

            var sessions = await query
                .OrderByDescending(gs => gs.StartedAt)
                .Take(limit)
                .ToListAsync();

            var result = new List<GameSessionSummaryDto>();

            foreach (var session in sessions)
            {
                var actionCount = await _context.GameActions
                    .Where(a => a.GameSessionId == session.Id)
                    .CountAsync();

                result.Add(new GameSessionSummaryDto
                {
                    Id = session.Id,
                    GameType = session.Lobby?.GameType ?? "Unknown",
                    StartedAt = session.StartedAt,
                    FinishedAt = session.FinishedAt,
                    TotalActions = actionCount,
                    PlayerCount = session.Lobby?.Players.Count ?? 0
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Récupère les actions d'un joueur spécifique
        /// </summary>
        [HttpGet("player-actions/{playerId}")]
        public async Task<ActionResult<List<GameActionDto>>> GetPlayerActions(
            Guid playerId,
            [FromQuery] Guid? gameSessionId = null)
        {
            var actions = await _historyService.GetPlayerActions(playerId, gameSessionId);

            if (!actions.Any())
            {
                return Ok(new List<GameActionDto>());
            }

            var result = actions.Select(a => new GameActionDto
            {
                Id = a.Id,
                PlayerPseudo = a.Player?.Pseudo ?? "Unknown",
                PlayerId = a.PlayerId,
                ActionType = a.ActionType,
                ActionData = a.ActionData,
                ParsedActionData = TryParseJson(a.ActionData),
                Timestamp = a.Timestamp,
                TimeSinceStart = FormatTimeSinceStart(a.GameSession?.StartedAt ?? a.Timestamp, a.Timestamp)
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Exporte l'historique d'une session en JSON (pour téléchargement)
        /// </summary>
        [HttpGet("export/{gameSessionId}")]
        public async Task<IActionResult> ExportGameHistory(Guid gameSessionId)
        {
            var historyResponse = await GetGameHistory(gameSessionId);

            if (historyResponse.Result is NotFoundObjectResult)
            {
                return NotFound("Game session not found");
            }

            var history = (historyResponse.Result as OkObjectResult)?.Value as GameHistoryResponse;

            if (history == null)
            {
                return NotFound();
            }

            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var fileName = $"game_history_{gameSessionId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
        }

        /// <summary>
        /// Statistiques globales de la plateforme
        /// </summary>
        [HttpGet("platform-stats")]
        public async Task<ActionResult<object>> GetPlatformStats()
        {
            var totalPlayers = await _context.Players.CountAsync();
            var totalGames = await _context.GameSessions.CountAsync();
            var totalActions = await _context.GameActions.CountAsync();
            var totalLobbies = await _context.Lobbies.CountAsync();

            var gamesByType = await _context.GameSessions
                .Include(gs => gs.Lobby)
                .GroupBy(gs => gs.Lobby!.GameType)
                .Select(g => new { GameType = g.Key, Count = g.Count() })
                .ToListAsync();

            var recentActivity = await _context.GameSessions
                .OrderByDescending(gs => gs.StartedAt)
                .Take(10)
                .Include(gs => gs.Lobby)
                .Select(gs => new
                {
                    gs.Id,
                    GameType = gs.Lobby!.GameType,
                    gs.StartedAt,
                    gs.FinishedAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalPlayers = totalPlayers,
                TotalGames = totalGames,
                TotalActions = totalActions,
                TotalLobbies = totalLobbies,
                GamesByType = gamesByType,
                RecentActivity = recentActivity
            });
        }

        /// <summary>
        /// Supprimer l'historique d'une session (GDPR)
        /// </summary>
        [HttpDelete("game-history/{gameSessionId}")]
        public async Task<ActionResult> DeleteGameHistory(Guid gameSessionId)
        {
            var actions = await _context.GameActions
                .Where(a => a.GameSessionId == gameSessionId)
                .ToListAsync();

            _context.GameActions.RemoveRange(actions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} actions for game session {GameSessionId}", 
                actions.Count, gameSessionId);

            return Ok(new { Deleted = actions.Count });
        }

        private static object? TryParseJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<object>(json);
            }
            catch
            {
                return json;
            }
        }

        private static string FormatTimeSinceStart(DateTime startTime, DateTime actionTime)
        {
            var elapsed = actionTime - startTime;
            
            if (elapsed.TotalHours >= 1)
                return $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            
            return $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        }
    }
}
