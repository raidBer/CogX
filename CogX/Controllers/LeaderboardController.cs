using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CogX.Data;
using CogX.Models;
using CogX.DTOs;

namespace CogX.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly CogXDbContext _context;

        public LeaderboardController(CogXDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère le classement pour un type de jeu
        /// </summary>
        /// <param name="gameType">Type de jeu (Morpion, Puissance4, SpeedTyping)</param>
        /// <param name="top">Nombre d'entrées à retourner (défaut: 10)</param>
        /// <param name="playerId">ID du joueur actuel (optionnel, pour le mettre en évidence)</param>
        [HttpGet("{gameType}")]
        public async Task<ActionResult<LeaderboardResponse>> GetLeaderboard(
            string gameType,
            [FromQuery] int top = 10,
            [FromQuery] Guid? playerId = null)
        {
            if (string.IsNullOrWhiteSpace(gameType))
            {
                return BadRequest("Game type is required");
            }

            // Récupérer toutes les entrées du jeu
            var allEntries = await _context.Leaderboard
                .Include(l => l.Player)
                .Where(l => l.GameType == gameType)
                .OrderByDescending(l => l.Score)
                .ThenBy(l => l.Time) // Pour Speed Typing (temps le plus court d'abord)
                .ThenBy(l => l.AchievedAt)
                .ToListAsync();

            var totalEntries = allEntries.Count;

            // Prendre le top N
            var topEntries = allEntries
                .Take(top)
                .Select((entry, index) => new LeaderboardDto
                {
                    Rank = index + 1,
                    Pseudo = entry.Player?.Pseudo ?? "Unknown",
                    Score = entry.Score,
                    Time = entry.Time,
                    TimeFormatted = FormatTime(entry.Time),
                    AchievedAt = entry.AchievedAt,
                    IsCurrentPlayer = playerId.HasValue && entry.PlayerId == playerId.Value
                })
                .ToList();

            // Chercher l'entrée du joueur actuel s'il n'est pas dans le top
            LeaderboardDto? currentPlayerEntry = null;
            if (playerId.HasValue)
            {
                var playerEntryIndex = allEntries.FindIndex(e => e.PlayerId == playerId.Value);
                if (playerEntryIndex >= 0 && playerEntryIndex >= top)
                {
                    var playerEntry = allEntries[playerEntryIndex];
                    currentPlayerEntry = new LeaderboardDto
                    {
                        Rank = playerEntryIndex + 1,
                        Pseudo = playerEntry.Player?.Pseudo ?? "You",
                        Score = playerEntry.Score,
                        Time = playerEntry.Time,
                        TimeFormatted = FormatTime(playerEntry.Time),
                        AchievedAt = playerEntry.AchievedAt,
                        IsCurrentPlayer = true
                    };
                }
            }

            return Ok(new LeaderboardResponse
            {
                GameType = gameType,
                Entries = topEntries,
                TotalEntries = totalEntries,
                CurrentPlayerEntry = currentPlayerEntry
            });
        }

        /// <summary>
        /// Récupère tous les types de jeux disponibles avec leurs statistiques
        /// </summary>
        [HttpGet("games")]
        public async Task<ActionResult<List<GameStatsDto>>> GetGameStats()
        {
            var stats = await _context.Leaderboard
                .GroupBy(l => l.GameType)
                .Select(g => new GameStatsDto
                {
                    GameType = g.Key,
                    TotalPlayers = g.Select(l => l.PlayerId).Distinct().Count(),
                    TotalGames = g.Count(),
                    HighestScore = g.Max(l => l.Score),
                    BestTime = g.Min(l => l.Time)
                })
                .ToListAsync();

            return Ok(stats);
        }

        /// <summary>
        /// Enregistre un nouveau score
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeaderboardDto>> AddScore(AddScoreRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.GameType))
            {
                return BadRequest("Game type is required");
            }

            var player = await _context.Players.FindAsync(request.PlayerId);
            if (player == null)
            {
                return NotFound("Player not found");
            }

            // Vérifier si c'est un record personnel
            var existingEntries = await _context.Leaderboard
                .Where(l => l.PlayerId == request.PlayerId && l.GameType == request.GameType)
                .ToListAsync();

            var isPersonalBest = !existingEntries.Any() ||
                                 request.Score > existingEntries.Max(e => e.Score) ||
                                 (request.Time.HasValue && existingEntries.All(e => !e.Time.HasValue || request.Time < e.Time));

            var entry = new LeaderboardEntry
            {
                Id = Guid.NewGuid(),
                GameType = request.GameType,
                PlayerId = request.PlayerId,
                Score = request.Score,
                Time = request.Time,
                AchievedAt = DateTime.UtcNow
            };

            _context.Leaderboard.Add(entry);
            await _context.SaveChangesAsync();

            // Calculer le rang
            var rank = await _context.Leaderboard
                .Where(l => l.GameType == request.GameType)
                .Where(l => l.Score > entry.Score || 
                           (l.Score == entry.Score && l.Time < entry.Time))
                .CountAsync() + 1;

            return Ok(new LeaderboardDto
            {
                Rank = rank,
                Pseudo = player.Pseudo,
                Score = entry.Score,
                Time = entry.Time,
                TimeFormatted = FormatTime(entry.Time),
                AchievedAt = entry.AchievedAt,
                IsCurrentPlayer = true
            });
        }

        /// <summary>
        /// Récupère l'historique des scores d'un joueur
        /// </summary>
        [HttpGet("player/{playerId}")]
        public async Task<ActionResult<List<LeaderboardDto>>> GetPlayerHistory(Guid playerId, [FromQuery] string? gameType = null)
        {
            var query = _context.Leaderboard
                .Include(l => l.Player)
                .Where(l => l.PlayerId == playerId);

            if (!string.IsNullOrWhiteSpace(gameType))
            {
                query = query.Where(l => l.GameType == gameType);
            }

            var entries = await query
                .OrderByDescending(l => l.AchievedAt)
                .Take(50) // Limiter à 50 dernières entrées
                .ToListAsync();

            var result = entries.Select(entry => new LeaderboardDto
            {
                Rank = 0, // Calculer si nécessaire
                Pseudo = entry.Player?.Pseudo ?? "Unknown",
                Score = entry.Score,
                Time = entry.Time,
                TimeFormatted = FormatTime(entry.Time),
                AchievedAt = entry.AchievedAt,
                IsCurrentPlayer = true
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Supprimer tous les scores d'un joueur (GDPR compliance)
        /// </summary>
        [HttpDelete("player/{playerId}")]
        public async Task<ActionResult> DeletePlayerScores(Guid playerId)
        {
            var entries = await _context.Leaderboard
                .Where(l => l.PlayerId == playerId)
                .ToListAsync();

            _context.Leaderboard.RemoveRange(entries);
            await _context.SaveChangesAsync();

            return Ok(new { Deleted = entries.Count });
        }

        private static string? FormatTime(TimeSpan? time)
        {
            if (!time.HasValue)
                return null;

            var t = time.Value;
            if (t.TotalMinutes >= 1)
                return $"{(int)t.TotalMinutes}m {t.Seconds}s";
            
            return t.TotalSeconds >= 10
                ? $"{(int)t.TotalSeconds}s"
                : $"{t.TotalSeconds:F1}s";
        }
    }

    public class GameStatsDto
    {
        public string GameType { get; set; } = string.Empty;
        public int TotalPlayers { get; set; }
        public int TotalGames { get; set; }
        public int HighestScore { get; set; }
        public TimeSpan? BestTime { get; set; }
    }
}
