using Microsoft.EntityFrameworkCore;
using MatchFynWebAPI.Data;
using MatchFynWebAPI.Models.Chat;

namespace MatchFynWebAPI.Services.Background
{
    /// <summary>
    /// Room management background service for MatchFyn
    /// Implements 7/24 automatic room management with professional architecture
    /// </summary>
    public class RoomManagementService : IRoomManagementService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RoomManagementService> _logger;

        public RoomManagementService(
            IServiceScopeFactory scopeFactory,
            ILogger<RoomManagementService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Clean up expired rooms (Waiting and Matching rooms with time limits)
        /// </summary>
        public async Task CleanupExpiredRoomsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var chatContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

                var expiredRooms = await chatContext.Rooms
                    .Where(r => r.IsActive && 
                               r.ExpiresAt.HasValue && 
                               r.ExpiresAt <= DateTime.UtcNow &&
                               (r.RoomType == RoomTypes.Waiting || r.RoomType == RoomTypes.Matching))
                    .ToListAsync();

                foreach (var room in expiredRooms)
                {
                    room.Status = RoomStatus.Expired;
                    room.IsActive = false;
                    room.UpdatedAt = DateTime.UtcNow;

                    // Deactivate all participants
                    var participants = await chatContext.RoomParticipants
                        .Where(p => p.RoomId == room.Id && p.IsActive)
                        .ToListAsync();

                    foreach (var participant in participants)
                    {
                        participant.IsActive = false;
                        participant.LeftAt = DateTime.UtcNow;
                        participant.Status = ParticipantStatus.Offline;
                    }

                    _logger.LogInformation("Expired room cleaned up: {RoomId} ({RoomType})", room.Id, room.RoomType);
                }

                if (expiredRooms.Any())
                {
                    await chatContext.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired rooms", expiredRooms.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired rooms");
            }
        }

        /// <summary>
        /// Create automatic waiting rooms based on gender and age groups
        /// </summary>
        public async Task CreateWaitingRoomsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var chatContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

                // Check if we need more waiting rooms
                var activeWaitingRooms = await chatContext.Rooms
                    .Where(r => r.IsActive && 
                               r.RoomType == RoomTypes.Waiting && 
                               r.Status == RoomStatus.Active)
                    .CountAsync();

                // Maintain at least 2 waiting rooms for each gender
                var genderFilters = new[] { "Male", "Female", "Mixed" };
                
                foreach (var gender in genderFilters)
                {
                    var genderRooms = await chatContext.Rooms
                        .Where(r => r.IsActive && 
                                   r.RoomType == RoomTypes.Waiting && 
                                   r.GenderFilter == gender &&
                                   r.Status == RoomStatus.Active)
                        .CountAsync();

                    if (genderRooms < 2) // Maintain minimum 2 rooms per gender
                    {
                        var newRoom = new Room
                        {
                            Name = $"Bekleme Odası - {GetGenderDisplayName(gender)}",
                            Description = $"{GetGenderDisplayName(gender)} için eşleşme bekleme odası",
                            RoomType = RoomTypes.Waiting,
                            Status = RoomStatus.Active,
                            MaxCapacity = 10,
                            GenderFilter = gender,
                            MinAge = 18,
                            MaxAge = 65,
                            DurationMinutes = 15,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                            IsPremium = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        chatContext.Rooms.Add(newRoom);
                        _logger.LogInformation("Created new waiting room for {Gender}", gender);
                    }
                }

                await chatContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating waiting rooms");
            }
        }

        /// <summary>
        /// Promote full waiting rooms to matching rooms
        /// </summary>
        public async Task PromoteWaitingRoomsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var chatContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

                var fullWaitingRooms = await chatContext.Rooms
                    .Include(r => r.Participants.Where(p => p.IsActive))
                    .Where(r => r.IsActive && 
                               r.RoomType == RoomTypes.Waiting && 
                               r.Status == RoomStatus.Active &&
                               r.CurrentParticipants >= r.MaxCapacity)
                    .ToListAsync();

                foreach (var waitingRoom in fullWaitingRooms)
                {
                    // Create new matching room
                    var matchingRoom = new Room
                    {
                        Name = $"Eşleşme Odası - {GetGenderDisplayName(waitingRoom.GenderFilter)}",
                        Description = "30 dakikalık eşleşme odası - Beğeni sistemi aktif!",
                        RoomType = RoomTypes.Matching,
                        Status = RoomStatus.Active,
                        MaxCapacity = 20,
                        GenderFilter = waitingRoom.GenderFilter,
                        MinAge = waitingRoom.MinAge,
                        MaxAge = waitingRoom.MaxAge,
                        DurationMinutes = 30,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                        IsPremium = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    chatContext.Rooms.Add(matchingRoom);
                    await chatContext.SaveChangesAsync(); // Save to get ID

                    // Move participants to matching room
                    foreach (var participant in waitingRoom.Participants.Where(p => p.IsActive))
                    {
                        // Create new participant in matching room
                        var newParticipant = new RoomParticipant
                        {
                            RoomId = matchingRoom.Id,
                            UserId = participant.UserId,
                            DisplayName = participant.DisplayName,
                            ProfileImageUrl = participant.ProfileImageUrl,
                            Role = ParticipantRoles.Member,
                            Status = ParticipantStatus.Online,
                            GridPosition = participant.GridPosition,
                            JoinedAt = DateTime.UtcNow,
                            LastActivityAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        chatContext.RoomParticipants.Add(newParticipant);

                        // Deactivate old participant
                        participant.IsActive = false;
                        participant.LeftAt = DateTime.UtcNow;
                    }

                    matchingRoom.CurrentParticipants = waitingRoom.CurrentParticipants;

                    // Close waiting room
                    waitingRoom.Status = RoomStatus.Closed;
                    waitingRoom.IsActive = false;
                    waitingRoom.CurrentParticipants = 0;
                    waitingRoom.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Promoted waiting room {WaitingRoomId} to matching room {MatchingRoomId}", 
                        waitingRoom.Id, matchingRoom.Id);
                }

                await chatContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting waiting rooms");
            }
        }

        /// <summary>
        /// Handle inactive participants (timeout after 5 minutes of inactivity)
        /// </summary>
        public async Task HandleInactiveParticipantsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var chatContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

                var inactiveThreshold = DateTime.UtcNow.AddMinutes(-5); // 5 minutes timeout

                var inactiveParticipants = await chatContext.RoomParticipants
                    .Include(p => p.Room)
                    .Where(p => p.IsActive && 
                               p.LastActivityAt < inactiveThreshold &&
                               p.Status != ParticipantStatus.Offline)
                    .ToListAsync();

                foreach (var participant in inactiveParticipants)
                {
                    participant.Status = ParticipantStatus.Away;
                    participant.IsMicrophoneEnabled = false;
                    participant.IsSpeaking = false;

                    // If inactive for more than 10 minutes, remove from room
                    if (participant.LastActivityAt < DateTime.UtcNow.AddMinutes(-10))
                    {
                        participant.IsActive = false;
                        participant.LeftAt = DateTime.UtcNow;
                        participant.Status = ParticipantStatus.Offline;

                        // Update room participant count
                        if (participant.Room != null)
                        {
                            participant.Room.CurrentParticipants = Math.Max(0, participant.Room.CurrentParticipants - 1);
                        }

                        _logger.LogInformation("Removed inactive participant {UserId} from room {RoomId}", 
                            participant.UserId, participant.RoomId);
                    }
                }

                if (inactiveParticipants.Any())
                {
                    await chatContext.SaveChangesAsync();
                    _logger.LogInformation("Handled {Count} inactive participants", inactiveParticipants.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling inactive participants");
            }
        }

        /// <summary>
        /// Create gender-based matching rooms when needed
        /// </summary>
        public async Task CreateMatchingRoomsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var chatContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

                var genderFilters = new[] { "Male", "Female", "Mixed" };

                foreach (var gender in genderFilters)
                {
                    var activeMatchingRooms = await chatContext.Rooms
                        .Where(r => r.IsActive && 
                                   r.RoomType == RoomTypes.Matching && 
                                   r.GenderFilter == gender &&
                                   r.Status == RoomStatus.Active)
                        .CountAsync();

                    // Maintain at least 1 matching room per gender
                    if (activeMatchingRooms == 0)
                    {
                        var newRoom = new Room
                        {
                            Name = $"Eşleşme Odası - {GetGenderDisplayName(gender)}",
                            Description = $"{GetGenderDisplayName(gender)} için eşleşme odası - Beğeni sistemi aktif!",
                            RoomType = RoomTypes.Matching,
                            Status = RoomStatus.Active,
                            MaxCapacity = 20,
                            GenderFilter = gender,
                            MinAge = 18,
                            MaxAge = 65,
                            DurationMinutes = 30,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                            IsPremium = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        chatContext.Rooms.Add(newRoom);
                        _logger.LogInformation("Created new matching room for {Gender}", gender);
                    }
                }

                await chatContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating matching rooms");
            }
        }

        /// <summary>
        /// Monitor room health and performance metrics
        /// </summary>
        public async Task MonitorRoomHealthAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var chatContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

                // Get room statistics
                var roomStats = await chatContext.Rooms
                    .Where(r => r.IsActive)
                    .GroupBy(r => r.RoomType)
                    .Select(g => new { RoomType = g.Key, Count = g.Count() })
                    .ToListAsync();

                var totalActiveParticipants = await chatContext.RoomParticipants
                    .CountAsync(p => p.IsActive);

                var totalActiveRooms = await chatContext.Rooms
                    .CountAsync(r => r.IsActive && r.Status == RoomStatus.Active);

                _logger.LogInformation("Room Health Check - Active Rooms: {ActiveRooms}, Active Participants: {ActiveParticipants}", 
                    totalActiveRooms, totalActiveParticipants);

                foreach (var stat in roomStats)
                {
                    _logger.LogInformation("Room Type: {RoomType}, Count: {Count}", stat.RoomType, stat.Count);
                }

                // Check for rooms with no participants for more than 30 minutes
                var emptyRooms = await chatContext.Rooms
                    .Where(r => r.IsActive && 
                               r.CurrentParticipants == 0 && 
                               r.UpdatedAt < DateTime.UtcNow.AddMinutes(-30) &&
                               r.RoomType != RoomTypes.Public) // Don't close public rooms
                    .ToListAsync();

                foreach (var emptyRoom in emptyRooms)
                {
                    emptyRoom.Status = RoomStatus.Inactive;
                    emptyRoom.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation("Marked empty room as inactive: {RoomId}", emptyRoom.Id);
                }

                if (emptyRooms.Any())
                {
                    await chatContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring room health");
            }
        }

        private static string GetGenderDisplayName(string? gender)
        {
            return gender switch
            {
                "Male" => "Erkek",
                "Female" => "Kadın",
                "Mixed" => "Karma",
                _ => "Genel"
            };
        }
    }
}
