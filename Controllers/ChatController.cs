using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MatchFynWebAPI.Data;
using MatchFynWebAPI.Models.Chat;

namespace MatchFynWebAPI.Controllers
{
    /// <summary>
    /// Chat controller for MatchFyn room management
    /// Implements TikTok Live-style room operations with professional architecture
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ChatDbContext _chatContext;
        private readonly ILogger<ChatController> _logger;

        public ChatController(ChatDbContext chatContext, ILogger<ChatController> logger)
        {
            _chatContext = chatContext ?? throw new ArgumentNullException(nameof(chatContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all active rooms with pagination
        /// </summary>
        [HttpGet("rooms")]
        public async Task<ActionResult<object>> GetRooms(
            [FromQuery] string? roomType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _chatContext.Rooms
                    .Where(r => r.IsActive && r.Status == RoomStatus.Active);

                if (!string.IsNullOrEmpty(roomType))
                {
                    query = query.Where(r => r.RoomType == roomType);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var rooms = await query
                    .Include(r => r.Participants.Where(p => p.IsActive))
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        RoomType = r.RoomType,
                        Status = r.Status,
                        CurrentParticipants = r.CurrentParticipants,
                        MaxCapacity = r.MaxCapacity,
                        GenderFilter = r.GenderFilter,
                        MinAge = r.MinAge,
                        MaxAge = r.MaxAge,
                        IsPremium = r.IsPremium,
                        Price = r.Price,
                        CreatedAt = r.CreatedAt,
                        Participants = r.Participants.Select(p => new
                        {
                            UserId = p.UserId,
                            DisplayName = p.DisplayName,
                            ProfileImageUrl = p.ProfileImageUrl,
                            Role = p.Role,
                            Status = p.Status,
                            GridPosition = p.GridPosition
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Rooms = rooms,
                    Pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = page < totalPages,
                        HasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rooms");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get room details by ID
        /// </summary>
        [HttpGet("rooms/{roomId}")]
        public async Task<ActionResult<object>> GetRoom(int roomId)
        {
            try
            {
                var room = await _chatContext.Rooms
                    .Include(r => r.Participants.Where(p => p.IsActive))
                    .Include(r => r.Messages.OrderByDescending(m => m.CreatedAt).Take(50))
                        .ThenInclude(m => m.Reactions)
                    .FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive);

                if (room == null)
                {
                    return NotFound(new { message = "Room not found" });
                }

                var roomData = new
                {
                    Id = room.Id,
                    Name = room.Name,
                    Description = room.Description,
                    RoomType = room.RoomType,
                    Status = room.Status,
                    CurrentParticipants = room.CurrentParticipants,
                    MaxCapacity = room.MaxCapacity,
                    GenderFilter = room.GenderFilter,
                    MinAge = room.MinAge,
                    MaxAge = room.MaxAge,
                    IsPremium = room.IsPremium,
                    Price = room.Price,
                    CreatedAt = room.CreatedAt,
                    Participants = room.Participants.Select(p => new
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        DisplayName = p.DisplayName,
                        ProfileImageUrl = p.ProfileImageUrl,
                        Role = p.Role,
                        Status = p.Status,
                        IsMicrophoneEnabled = p.IsMicrophoneEnabled,
                        IsSpeaking = p.IsSpeaking,
                        GridPosition = p.GridPosition,
                        JoinedAt = p.JoinedAt
                    }).OrderBy(p => p.GridPosition).ToList(),
                    RecentMessages = room.Messages.Select(m => new
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        Content = m.Content,
                        MessageType = m.MessageType,
                        CreatedAt = m.CreatedAt,
                        ReactionCounts = m.Reactions.GroupBy(r => r.ReactionType)
                            .Select(g => new { ReactionType = g.Key, Count = g.Count() })
                            .ToList()
                    }).ToList()
                };

                return Ok(roomData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room {RoomId}", roomId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create a new room
        /// </summary>
        [HttpPost("rooms")]
        public async Task<ActionResult<object>> CreateRoom([FromBody] CreateRoomDto createRoomDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                // Validate room type settings
                var defaultSettings = RoomTypes.GetDefaultSettings();
                if (!defaultSettings.ContainsKey(createRoomDto.RoomType))
                {
                    return BadRequest(new { message = "Invalid room type" });
                }

                var (defaultMaxCapacity, defaultDuration) = defaultSettings[createRoomDto.RoomType];

                var room = new Room
                {
                    Name = createRoomDto.Name.Trim(),
                    Description = createRoomDto.Description?.Trim(),
                    RoomType = createRoomDto.RoomType,
                    Status = RoomStatus.Active,
                    MaxCapacity = createRoomDto.MaxCapacity ?? defaultMaxCapacity,
                    CreatedByUserId = userId,
                    GenderFilter = createRoomDto.GenderFilter,
                    MinAge = createRoomDto.MinAge,
                    MaxAge = createRoomDto.MaxAge,
                    DurationMinutes = createRoomDto.DurationMinutes ?? defaultDuration,
                    ExpiresAt = createRoomDto.DurationMinutes.HasValue 
                        ? DateTime.UtcNow.AddMinutes(createRoomDto.DurationMinutes.Value) 
                        : null,
                    IsPremium = createRoomDto.IsPremium,
                    Price = createRoomDto.Price,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _chatContext.Rooms.Add(room);
                await _chatContext.SaveChangesAsync();

                _logger.LogInformation("Room created: {RoomId} by user {UserId}", room.Id, userId);

                return CreatedAtAction(nameof(GetRoom), new { roomId = room.Id }, new
                {
                    Id = room.Id,
                    Name = room.Name,
                    Description = room.Description,
                    RoomType = room.RoomType,
                    MaxCapacity = room.MaxCapacity,
                    CreatedAt = room.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get room messages with pagination
        /// </summary>
        [HttpGet("rooms/{roomId}/messages")]
        public async Task<ActionResult<object>> GetRoomMessages(
            int roomId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Verify user has access to room
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var participant = await _chatContext.RoomParticipants
                    .AnyAsync(p => p.RoomId == roomId && p.UserId == userId);

                if (!participant)
                {
                    return Forbid("You don't have access to this room");
                }

                var totalCount = await _chatContext.Messages
                    .CountAsync(m => m.RoomId == roomId && !m.IsDeleted);

                var messages = await _chatContext.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Reactions)
                    .Where(m => m.RoomId == roomId && !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.DisplayName,
                        SenderProfileImage = m.Sender.ProfileImageUrl,
                        Content = m.Content,
                        MessageType = m.MessageType,
                        CreatedAt = m.CreatedAt,
                        IsEdited = m.IsEdited,
                        ReactionCounts = m.Reactions.GroupBy(r => r.ReactionType)
                            .Select(g => new { ReactionType = g.Key, Count = g.Count() })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Messages = messages.OrderBy(m => m.CreatedAt).ToList(), // Reverse for chronological order
                    Pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for room {RoomId}", roomId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user's active rooms
        /// </summary>
        [HttpGet("my-rooms")]
        public async Task<ActionResult<object>> GetMyRooms()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var myRooms = await _chatContext.RoomParticipants
                    .Include(p => p.Room)
                    .Where(p => p.UserId == userId && p.IsActive && p.Room.IsActive)
                    .Select(p => new
                    {
                        Room = new
                        {
                            Id = p.Room.Id,
                            Name = p.Room.Name,
                            Description = p.Room.Description,
                            RoomType = p.Room.RoomType,
                            CurrentParticipants = p.Room.CurrentParticipants,
                            MaxCapacity = p.Room.MaxCapacity
                        },
                        MyRole = p.Role,
                        JoinedAt = p.JoinedAt,
                        LastActivityAt = p.LastActivityAt
                    })
                    .OrderByDescending(p => p.LastActivityAt)
                    .ToListAsync();

                return Ok(new { MyRooms = myRooms });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user rooms for {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

    /// <summary>
    /// DTO for creating new rooms
    /// </summary>
    public class CreateRoomDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RoomType { get; set; } = RoomTypes.Public;
        public int? MaxCapacity { get; set; }
        public string? GenderFilter { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public int? DurationMinutes { get; set; }
        public bool IsPremium { get; set; } = false;
        public decimal? Price { get; set; }
    }
}
