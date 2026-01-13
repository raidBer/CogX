using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CogX.Data;
using CogX.Models;
using CogX.DTOs;
using CogX.Hubs;

namespace CogX.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LobbyController : ControllerBase
    {
        private readonly CogXDbContext _context;
        private readonly IHubContext<LobbyHub> _hubContext;

        public LobbyController(CogXDbContext context, IHubContext<LobbyHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET /api/lobby - Liste des lobbies publics en attente
        [HttpGet]
        public async Task<ActionResult<List<LobbyDto>>> GetPublicLobbies([FromQuery] string? gameType = null)
        {
            var query = _context.Lobbies
                .Include(l => l.Host)
                .Include(l => l.Players)
                .Where(l => l.Status == LobbyStatus.Waiting && l.Password == null);

            if (!string.IsNullOrWhiteSpace(gameType))
            {
                query = query.Where(l => l.GameType == gameType);
            }

            var lobbies = await query
                .Select(l => new LobbyDto
                {
                    Id = l.Id,
                    GameType = l.GameType,
                    HostPseudo = l.Host!.Pseudo,
                    CurrentPlayers = l.Players.Count,
                    MaxPlayers = l.MaxPlayers,
                    IsPrivate = false
                })
                .ToListAsync();

            return Ok(lobbies);
        }

        // GET /api/lobby/{id} - Détails d'un lobby
        [HttpGet("{id}")]
        public async Task<ActionResult<LobbyDetailsDto>> GetLobby(Guid id, [FromQuery] Guid playerId)
        {
            var lobby = await _context.Lobbies
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lobby == null)
            {
                return NotFound("Lobby not found");
            }

            return Ok(new LobbyDetailsDto
            {
                Id = lobby.Id,
                GameType = lobby.GameType,
                Players = lobby.Players.Select(p => new PlayerDto
                {
                    Id = p.Id,
                    Pseudo = p.Pseudo
                }).ToList(),
                MaxPlayers = lobby.MaxPlayers,
                IsHost = lobby.HostId == playerId,
                Status = lobby.Status.ToString()
            });
        }

        // GET /api/lobby/{id}/players - Liste des joueurs dans un lobby
        [HttpGet("{id}/players")]
        public async Task<ActionResult<List<PlayerDto>>> GetLobbyPlayers(Guid id)
        {
            var lobby = await _context.Lobbies
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lobby == null)
            {
                return NotFound("Lobby not found");
            }

            var players = lobby.Players.Select(p => new PlayerDto
            {
                Id = p.Id,
                Pseudo = p.Pseudo
            }).ToList();

            return Ok(players);
        }

        // POST /api/lobby - Créer un lobby
        [HttpPost]
        public async Task<ActionResult<CreateLobbyResponse>> CreateLobby(CreateLobbyRequest request)
        {
            var player = await _context.Players.FindAsync(request.PlayerId);
            if (player == null)
            {
                return NotFound("Player not found");
            }

            var lobby = new Lobby
            {
                Id = Guid.NewGuid(),
                GameType = request.GameType,
                HostId = request.PlayerId,
                Password = request.Password,
                Status = LobbyStatus.Waiting,
                MaxPlayers = request.MaxPlayers,
                CreatedAt = DateTime.UtcNow
            };

            lobby.Players.Add(player);
            _context.Lobbies.Add(lobby);
            await _context.SaveChangesAsync();

            // Notifier tous les clients qu'un nouveau lobby est disponible
            await _hubContext.Clients.All.SendAsync("LobbyCreated", new LobbyDto
            {
                Id = lobby.Id,
                GameType = lobby.GameType,
                HostPseudo = player.Pseudo,
                CurrentPlayers = 1,
                MaxPlayers = lobby.MaxPlayers,
                IsPrivate = !string.IsNullOrEmpty(lobby.Password)
            });

            var shareLink = $"{Request.Scheme}://{Request.Host}/join/{lobby.Id}";
            return Ok(new CreateLobbyResponse
            {
                LobbyId = lobby.Id,
                ShareLink = shareLink
            });
        }

        // POST /api/lobby/{id}/join - Rejoindre un lobby
        [HttpPost("{id}/join")]
        public async Task<ActionResult> JoinLobby(Guid id, JoinLobbyRequest request)
        {
            var lobby = await _context.Lobbies
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lobby == null)
            {
                return NotFound("Lobby not found");
            }

            if (lobby.Status != LobbyStatus.Waiting)
            {
                return BadRequest("Lobby has already started");
            }

            if (lobby.Players.Count >= lobby.MaxPlayers)
            {
                return BadRequest("Lobby is full");
            }

            if (!string.IsNullOrEmpty(lobby.Password) && lobby.Password != request.Password)
            {
                return Unauthorized("Invalid password");
            }

            var player = await _context.Players.FindAsync(request.PlayerId);
            if (player == null)
            {
                return NotFound("Player not found");
            }

            if (lobby.Players.Any(p => p.Id == player.Id))
            {
                return BadRequest("Player already in lobby");
            }

            lobby.Players.Add(player);
            await _context.SaveChangesAsync();

            // Notifier tous les joueurs du lobby
            await _hubContext.Clients.Group(id.ToString())
                .SendAsync("PlayerJoined", new PlayerDto
                {
                    Id = player.Id,
                    Pseudo = player.Pseudo
                });

            return Ok();
        }

        // POST /api/lobby/{id}/leave - Quitter un lobby
        [HttpPost("{id}/leave")]
        public async Task<ActionResult> LeaveLobby(Guid id, [FromBody] Guid playerId)
        {
            var lobby = await _context.Lobbies
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lobby == null)
            {
                return NotFound("Lobby not found");
            }

            if (lobby.Status != LobbyStatus.Waiting)
            {
                return BadRequest("Cannot leave a lobby that has already started");
            }

            var player = lobby.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                return NotFound("Player not in this lobby");
            }

            // Si c'est le host qui part, supprimer le lobby
            if (lobby.HostId == playerId)
            {
                _context.Lobbies.Remove(lobby);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("LobbyDeleted", id);
                await _hubContext.Clients.Group(id.ToString()).SendAsync("LobbyClosed", "Host left the lobby");

                return Ok(new { Message = "Lobby deleted (host left)" });
            }

            // Sinon, retirer le joueur
            lobby.Players.Remove(player);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(id.ToString())
                .SendAsync("PlayerLeft", new PlayerDto
                {
                    Id = player.Id,
                    Pseudo = player.Pseudo
                });

            return Ok();
        }

        // POST /api/lobby/{id}/start - Démarrer la partie (host uniquement)
        [HttpPost("{id}/start")]
        public async Task<ActionResult> StartGame(Guid id, [FromBody] Guid hostId)
        {
            var lobby = await _context.Lobbies
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lobby == null)
            {
                return NotFound("Lobby not found");
            }

            if (lobby.HostId != hostId)
            {
                return Forbid("Only the host can start the game");
            }

            if (lobby.Status != LobbyStatus.Waiting)
            {
                return BadRequest("Game has already started");
            }

            lobby.Status = LobbyStatus.InProgress;
            await _context.SaveChangesAsync();

            // Créer une session de jeu
            var gameSession = new GameSession
            {
                Id = Guid.NewGuid(),
                LobbyId = lobby.Id,
                GameState = "{}",
                StartedAt = DateTime.UtcNow
            };
            _context.GameSessions.Add(gameSession);
            await _context.SaveChangesAsync();

            // Notifier tous les joueurs
            await _hubContext.Clients.Group(id.ToString())
                .SendAsync("GameStarted", new
                {
                    GameType = lobby.GameType,
                    GameSessionId = gameSession.Id,
                    Players = lobby.Players.Select(p => new PlayerDto
                    {
                        Id = p.Id,
                        Pseudo = p.Pseudo
                    }).ToList()
                });

            return Ok(new { GameSessionId = gameSession.Id });
        }

        // DELETE /api/lobby/{id} - Supprimer un lobby (host uniquement)
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteLobby(Guid id, [FromQuery] Guid hostId)
        {
            var lobby = await _context.Lobbies.FindAsync(id);

            if (lobby == null)
            {
                return NotFound("Lobby not found");
            }

            if (lobby.HostId != hostId)
            {
                return Forbid("Only the host can delete the lobby");
            }

            _context.Lobbies.Remove(lobby);
            await _context.SaveChangesAsync();

            // Notifier tous les clients
            await _hubContext.Clients.All.SendAsync("LobbyDeleted", id);
            await _hubContext.Clients.Group(id.ToString()).SendAsync("LobbyClosed");

            return Ok();
        }
    }
}
