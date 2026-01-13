using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CogX.Data;
using CogX.Models;
using CogX.DTOs;

namespace CogX.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly CogXDbContext _context;

        public PlayerController(CogXDbContext context)
        {
            _context = context;
        }

        // GET /api/player - Liste tous les joueurs
        [HttpGet]
        public async Task<ActionResult<List<PlayerDto>>> GetAllPlayers([FromQuery] int limit = 100)
        {
            var players = await _context.Players
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .Select(p => new PlayerDto
                {
                    Id = p.Id,
                    Pseudo = p.Pseudo
                })
                .ToListAsync();

            return Ok(players);
        }

        // POST /api/player - Créer un joueur (saisir le pseudo)
        [HttpPost]
        public async Task<ActionResult<PlayerDto>> CreatePlayer(CreatePlayerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Pseudo))
            {
                return BadRequest("Pseudo is required");
            }

            var player = new Player
            {
                Id = Guid.NewGuid(),
                Pseudo = request.Pseudo,
                CreatedAt = DateTime.UtcNow
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return Ok(new PlayerDto
            {
                Id = player.Id,
                Pseudo = player.Pseudo
            });
        }

        // GET /api/player/{id} - Récupérer un joueur
        [HttpGet("{id}")]
        public async Task<ActionResult<PlayerDto>> GetPlayer(Guid id)
        {
            var player = await _context.Players.FindAsync(id);

            if (player == null)
            {
                return NotFound("Player not found");
            }

            return Ok(new PlayerDto
            {
                Id = player.Id,
                Pseudo = player.Pseudo
            });
        }

        // DELETE /api/player/{id} - Supprimer un joueur (GDPR)
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePlayer(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound("Player not found");
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Player deleted successfully" });
        }
    }
}
