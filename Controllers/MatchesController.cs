using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MatchFynWebAPI.Data;
using MatchFynWebAPI.Models;
using MatchFynWebAPI.DTOs;

namespace MatchFynWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchesController : ControllerBase
    {
        private readonly MatchFynDbContext _context;

        public MatchesController(MatchFynDbContext context)
        {
            _context = context;
        }

        // GET: api/Matches/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<MatchDto>>> GetUserMatches(int userId)
        {
            var matches = await _context.Matches
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => new MatchDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.Name,
                    SenderProfileImage = m.Sender.ProfileImageUrl,
                    ReceiverId = m.ReceiverId,
                    ReceiverName = m.Receiver.Name,
                    ReceiverProfileImage = m.Receiver.ProfileImageUrl,
                    Status = m.Status,
                    CreatedAt = m.CreatedAt,
                    RespondedAt = m.RespondedAt,
                    Message = m.Message
                })
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Ok(matches);
        }

        // GET: api/Matches/pending/5
        [HttpGet("pending/{userId}")]
        public async Task<ActionResult<IEnumerable<MatchDto>>> GetPendingMatches(int userId)
        {
            var pendingMatches = await _context.Matches
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == userId && m.Status == "pending")
                .Select(m => new MatchDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.Name,
                    SenderProfileImage = m.Sender.ProfileImageUrl,
                    ReceiverId = m.ReceiverId,
                    ReceiverName = m.Receiver.Name,
                    ReceiverProfileImage = m.Receiver.ProfileImageUrl,
                    Status = m.Status,
                    CreatedAt = m.CreatedAt,
                    RespondedAt = m.RespondedAt,
                    Message = m.Message
                })
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Ok(pendingMatches);
        }

        // GET: api/Matches/accepted/5
        [HttpGet("accepted/{userId}")]
        public async Task<ActionResult<IEnumerable<MatchDto>>> GetAcceptedMatches(int userId)
        {
            var acceptedMatches = await _context.Matches
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && m.Status == "accepted")
                .Select(m => new MatchDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.Name,
                    SenderProfileImage = m.Sender.ProfileImageUrl,
                    ReceiverId = m.ReceiverId,
                    ReceiverName = m.Receiver.Name,
                    ReceiverProfileImage = m.Receiver.ProfileImageUrl,
                    Status = m.Status,
                    CreatedAt = m.CreatedAt,
                    RespondedAt = m.RespondedAt,
                    Message = m.Message
                })
                .OrderByDescending(m => m.RespondedAt)
                .ToListAsync();

            return Ok(acceptedMatches);
        }

        // POST: api/Matches
        [HttpPost]
        public async Task<ActionResult<MatchDto>> CreateMatch(int senderId, CreateMatchDto createMatchDto)
        {
            // Check if sender exists
            var sender = await _context.Users.FindAsync(senderId);
            if (sender == null || !sender.IsActive)
            {
                return BadRequest("Sender not found");
            }

            // Check if receiver exists
            var receiver = await _context.Users.FindAsync(createMatchDto.ReceiverId);
            if (receiver == null || !receiver.IsActive)
            {
                return BadRequest("Receiver not found");
            }

            // Check if match already exists
            var existingMatch = await _context.Matches
                .FirstOrDefaultAsync(m => 
                    (m.SenderId == senderId && m.ReceiverId == createMatchDto.ReceiverId) ||
                    (m.SenderId == createMatchDto.ReceiverId && m.ReceiverId == senderId));

            if (existingMatch != null)
            {
                return BadRequest("Match already exists between these users");
            }

            var match = new Match
            {
                SenderId = senderId,
                ReceiverId = createMatchDto.ReceiverId,
                Message = createMatchDto.Message,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            var matchDto = new MatchDto
            {
                Id = match.Id,
                SenderId = match.SenderId,
                SenderName = sender.Name,
                SenderProfileImage = sender.ProfileImageUrl,
                ReceiverId = match.ReceiverId,
                ReceiverName = receiver.Name,
                ReceiverProfileImage = receiver.ProfileImageUrl,
                Status = match.Status,
                CreatedAt = match.CreatedAt,
                RespondedAt = match.RespondedAt,
                Message = match.Message
            };

            return CreatedAtAction(nameof(GetUserMatches), new { userId = senderId }, matchDto);
        }

        // PUT: api/Matches/5/respond
        [HttpPut("{matchId}/respond")]
        public async Task<IActionResult> RespondToMatch(int matchId, RespondToMatchDto respondDto)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null)
            {
                return NotFound();
            }

            if (match.Status != "pending")
            {
                return BadRequest("Match is not in pending status");
            }

            match.Status = respondDto.Status;
            match.RespondedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MatchExists(matchId))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // GET: api/Matches/suggestions/5
        [HttpGet("suggestions/{userId}")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetMatchSuggestions(int userId)
        {
            // Get user's interests
            var userInterests = await _context.UserInterests
                .Where(ui => ui.UserId == userId)
                .Select(ui => ui.InterestId)
                .ToListAsync();

            // Get users who already have matches with current user
            var existingMatchUserIds = await _context.Matches
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .ToListAsync();

            // Find suggested users based on common interests
            var suggestions = await _context.Users
                .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Interest)
                .Where(u => u.Id != userId && 
                           u.IsActive && 
                           !existingMatchUserIds.Contains(u.Id) &&
                           u.UserInterests.Any(ui => userInterests.Contains(ui.InterestId)))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender,
                    Bio = u.Bio,
                    ProfileImageUrl = u.ProfileImageUrl,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive,
                    Interests = u.UserInterests.Select(ui => ui.Interest.Name).ToList()
                })
                .Take(10)
                .ToListAsync();

            return Ok(suggestions);
        }

        private bool MatchExists(int id)
        {
            return _context.Matches.Any(e => e.Id == id);
        }
    }
}
