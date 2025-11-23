using CogX.Data;
using CogX.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CogX.Services
{
    public interface IGameHistoryService
    {
        Task LogAction(Guid gameSessionId, Guid playerId, string actionType, object actionData);
        Task<List<GameAction>> GetGameHistory(Guid gameSessionId);
        Task<List<GameAction>> GetPlayerActions(Guid playerId, Guid? gameSessionId = null);
        Task<Dictionary<string, int>> GetActionStats(Guid gameSessionId);
    }

    public class GameHistoryService : IGameHistoryService
    {
        private readonly CogXDbContext _context;
        private readonly ILogger<GameHistoryService> _logger;

        public GameHistoryService(CogXDbContext context, ILogger<GameHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Enregistre une action de jeu
        /// </summary>
        public async Task LogAction(Guid gameSessionId, Guid playerId, string actionType, object actionData)
        {
            try
            {
                var action = new GameAction
                {
                    Id = Guid.NewGuid(),
                    GameSessionId = gameSessionId,
                    PlayerId = playerId,
                    ActionType = actionType,
                    ActionData = JsonSerializer.Serialize(actionData),
                    Timestamp = DateTime.UtcNow
                };

                _context.GameActions.Add(action);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Action logged: {ActionType} by player {PlayerId} in session {GameSessionId}",
                    actionType, playerId, gameSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log action: {ActionType}", actionType);
                // Ne pas faire échouer le jeu si le logging échoue
            }
        }

        /// <summary>
        /// Récupère l'historique complet d'une session de jeu
        /// </summary>
        public async Task<List<GameAction>> GetGameHistory(Guid gameSessionId)
        {
            return await _context.GameActions
                .Include(a => a.Player)
                .Include(a => a.GameSession)
                .Where(a => a.GameSessionId == gameSessionId)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les actions d'un joueur spécifique
        /// </summary>
        public async Task<List<GameAction>> GetPlayerActions(Guid playerId, Guid? gameSessionId = null)
        {
            var query = _context.GameActions
                .Include(a => a.Player)
                .Include(a => a.GameSession)
                .Where(a => a.PlayerId == playerId);

            if (gameSessionId.HasValue)
            {
                query = query.Where(a => a.GameSessionId == gameSessionId.Value);
            }

            return await query
                .OrderBy(a => a.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère des statistiques sur les actions d'une session
        /// </summary>
        public async Task<Dictionary<string, int>> GetActionStats(Guid gameSessionId)
        {
            var actions = await _context.GameActions
                .Where(a => a.GameSessionId == gameSessionId)
                .GroupBy(a => a.ActionType)
                .Select(g => new { ActionType = g.Key, Count = g.Count() })
                .ToListAsync();

            return actions.ToDictionary(a => a.ActionType, a => a.Count);
        }
    }
}
