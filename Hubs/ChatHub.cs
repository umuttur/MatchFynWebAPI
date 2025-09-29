using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MatchFynWebAPI.Data;
using MatchFynWebAPI.Models.Chat;

namespace MatchFynWebAPI.Hubs
{
    /// <summary>
    /// MatchFyn Chat Hub for real-time communication
    /// Implements TikTok Live-style features with professional architecture
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatDbContext _chatContext;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ChatDbContext chatContext, ILogger<ChatHub> logger)
        {
            _chatContext = chatContext ?? throw new ArgumentNullException(nameof(chatContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Connection Management

        /// <summary>
        /// Handle user connection
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                _logger.LogInformation("User connected: {UserId} ({UserName}) - Connection: {ConnectionId}", 
                    userId, userName, Context.ConnectionId);

                // Add user to their personal group for direct messaging
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                // Notify about user online status
                await Clients.All.SendAsync("UserOnline", new { UserId = userId, UserName = userName });

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        /// <summary>
        /// Handle user disconnection
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                _logger.LogInformation("User disconnected: {UserId} ({UserName}) - Connection: {ConnectionId}", 
                    userId, userName, Context.ConnectionId);

                // Update participant status in all rooms
                await UpdateParticipantStatusOnDisconnect(userId);

                // Notify about user offline status
                await Clients.All.SendAsync("UserOffline", new { UserId = userId, UserName = userName });

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        #endregion

        #region Room Management

        /// <summary>
        /// Join a chat room (TikTok Live style)
        /// </summary>
        public async Task JoinRoom(int roomId)
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                var room = await _chatContext.Rooms
                    .Include(r => r.Participants)
                    .FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive);

                if (room == null)
                {
                    await Clients.Caller.SendAsync("Error", "Room not found or inactive");
                    return;
                }

                // Check room capacity
                if (room.CurrentParticipants >= room.MaxCapacity)
                {
                    await Clients.Caller.SendAsync("Error", "Room is full");
                    return;
                }

                // Check if user is already in room
                var existingParticipant = await _chatContext.RoomParticipants
                    .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId && p.IsActive);

                if (existingParticipant != null)
                {
                    // Update connection ID
                    existingParticipant.ConnectionId = Context.ConnectionId;
                    existingParticipant.Status = ParticipantStatus.Online;
                    existingParticipant.LastActivityAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new participant
                    var participant = new RoomParticipant
                    {
                        RoomId = roomId,
                        UserId = userId,
                        DisplayName = userName,
                        ConnectionId = Context.ConnectionId,
                        Role = room.CreatedByUserId == userId ? ParticipantRoles.Owner : ParticipantRoles.Member,
                        Status = ParticipantStatus.Online,
                        GridPosition = GetNextAvailableGridPosition(room.Participants.Where(p => p.IsActive).ToList()),
                        JoinedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _chatContext.RoomParticipants.Add(participant);
                    room.CurrentParticipants++;
                }

                await _chatContext.SaveChangesAsync();

                // Add to SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");

                // Get updated room info
                var roomInfo = await GetRoomInfo(roomId);

                // Notify room about new participant (TikTok Live style)
                await Clients.Group($"room_{roomId}").SendAsync("UserJoinedRoom", new
                {
                    UserId = userId,
                    UserName = userName,
                    RoomInfo = roomInfo,
                    Message = $"{userName} odaya katıldı! 🎉"
                });

                // Send room info to the joining user
                await Clients.Caller.SendAsync("JoinedRoom", roomInfo);

                // Add system message
                await AddSystemMessage(roomId, $"{userName} odaya katıldı", MessageTypes.Join);

                _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining room {RoomId} for user {UserId}", roomId, GetUserId());
                await Clients.Caller.SendAsync("Error", "Failed to join room");
            }
        }

        /// <summary>
        /// Leave a chat room
        /// </summary>
        public async Task LeaveRoom(int roomId)
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                var participant = await _chatContext.RoomParticipants
                    .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId && p.IsActive);

                if (participant != null)
                {
                    // Calculate total time in room
                    var totalMinutes = (int)(DateTime.UtcNow - participant.JoinedAt).TotalMinutes;
                    participant.TotalTimeMinutes = totalMinutes;
                    participant.LeftAt = DateTime.UtcNow;
                    participant.IsActive = false;

                    // Update room participant count
                    var room = await _chatContext.Rooms.FindAsync(roomId);
                    if (room != null)
                    {
                        room.CurrentParticipants = Math.Max(0, room.CurrentParticipants - 1);
                    }

                    await _chatContext.SaveChangesAsync();

                    // Remove from SignalR group
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");

                    // Notify room about participant leaving
                    await Clients.Group($"room_{roomId}").SendAsync("UserLeftRoom", new
                    {
                        UserId = userId,
                        UserName = userName,
                        Message = $"{userName} odadan ayrıldı"
                    });

                    // Add system message
                    await AddSystemMessage(roomId, $"{userName} odadan ayrıldı", MessageTypes.Leave);

                    _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving room {RoomId} for user {UserId}", roomId, GetUserId());
            }
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Send message to room (TikTok Live style chat)
        /// </summary>
        public async Task SendMessage(int roomId, string content, string messageType = "Text")
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                // Validate user is in room
                var participant = await _chatContext.RoomParticipants
                    .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId && p.IsActive);

                if (participant == null)
                {
                    await Clients.Caller.SendAsync("Error", "You are not in this room");
                    return;
                }

                // Create message
                var message = new Message
                {
                    RoomId = roomId,
                    SenderId = participant.Id,
                    Content = content.Trim(),
                    MessageType = messageType,
                    CreatedAt = DateTime.UtcNow
                };

                _chatContext.Messages.Add(message);

                // Update participant activity
                participant.LastActivityAt = DateTime.UtcNow;

                await _chatContext.SaveChangesAsync();

                // Prepare message for broadcast
                var messageData = new
                {
                    Id = message.Id,
                    RoomId = roomId,
                    SenderId = participant.Id,
                    SenderUserId = userId,
                    SenderName = userName,
                    SenderProfileImage = participant.ProfileImageUrl,
                    Content = content,
                    MessageType = messageType,
                    CreatedAt = message.CreatedAt,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                // Broadcast to room (TikTok Live style)
                await Clients.Group($"room_{roomId}").SendAsync("NewMessage", messageData);

                _logger.LogInformation("Message sent in room {RoomId} by user {UserId}", roomId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to room {RoomId} by user {UserId}", roomId, GetUserId());
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        /// <summary>
        /// React to message (TikTok-style hearts and reactions)
        /// </summary>
        public async Task ReactToMessage(int messageId, string reactionType)
        {
            try
            {
                var userId = GetUserId();

                var message = await _chatContext.Messages
                    .Include(m => m.Reactions)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("Error", "Message not found");
                    return;
                }

                // Check if user already reacted with this type
                var existingReaction = message.Reactions
                    .FirstOrDefault(r => r.UserId == userId && r.ReactionType == reactionType);

                if (existingReaction != null)
                {
                    // Remove existing reaction (toggle)
                    _chatContext.MessageReactions.Remove(existingReaction);
                }
                else
                {
                    // Add new reaction
                    var reaction = new MessageReaction
                    {
                        MessageId = messageId,
                        UserId = userId,
                        ReactionType = reactionType,
                        Emoji = ReactionTypes.GetReactionEmojis().GetValueOrDefault(reactionType, "❤️"),
                        CreatedAt = DateTime.UtcNow
                    };

                    _chatContext.MessageReactions.Add(reaction);
                }

                await _chatContext.SaveChangesAsync();

                // Get updated reaction counts
                var reactionCounts = await _chatContext.MessageReactions
                    .Where(r => r.MessageId == messageId)
                    .GroupBy(r => r.ReactionType)
                    .Select(g => new { ReactionType = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Broadcast reaction update (TikTok-style animation trigger)
                await Clients.Group($"room_{message.RoomId}").SendAsync("MessageReactionUpdate", new
                {
                    MessageId = messageId,
                    ReactionType = reactionType,
                    UserId = userId,
                    ReactionCounts = reactionCounts,
                    IsAdded = existingReaction == null
                });

                _logger.LogInformation("Reaction {ReactionType} on message {MessageId} by user {UserId}", 
                    reactionType, messageId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to message {MessageId} by user {UserId}", messageId, GetUserId());
            }
        }

        #endregion

        #region Voice Chat

        /// <summary>
        /// Toggle microphone (TikTok Live style)
        /// </summary>
        public async Task ToggleMicrophone(int roomId, bool isEnabled)
        {
            try
            {
                var userId = GetUserId();

                var participant = await _chatContext.RoomParticipants
                    .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId && p.IsActive);

                if (participant != null)
                {
                    participant.IsMicrophoneEnabled = isEnabled;
                    participant.LastActivityAt = DateTime.UtcNow;

                    await _chatContext.SaveChangesAsync();

                    // Notify room about microphone status
                    await Clients.Group($"room_{roomId}").SendAsync("MicrophoneToggled", new
                    {
                        UserId = userId,
                        IsEnabled = isEnabled,
                        ParticipantId = participant.Id
                    });

                    _logger.LogInformation("User {UserId} toggled microphone to {IsEnabled} in room {RoomId}", 
                        userId, isEnabled, roomId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling microphone for user {UserId} in room {RoomId}", GetUserId(), roomId);
            }
        }

        /// <summary>
        /// Update speaking status (for voice activity indicators)
        /// </summary>
        public async Task UpdateSpeakingStatus(int roomId, bool isSpeaking)
        {
            try
            {
                var userId = GetUserId();

                var participant = await _chatContext.RoomParticipants
                    .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId && p.IsActive);

                if (participant != null)
                {
                    participant.IsSpeaking = isSpeaking;
                    participant.Status = isSpeaking ? ParticipantStatus.Speaking : ParticipantStatus.Online;
                    participant.LastActivityAt = DateTime.UtcNow;

                    await _chatContext.SaveChangesAsync();

                    // Notify room about speaking status (for visual indicators)
                    await Clients.Group($"room_{roomId}").SendAsync("SpeakingStatusUpdate", new
                    {
                        UserId = userId,
                        IsSpeaking = isSpeaking,
                        ParticipantId = participant.Id
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating speaking status for user {UserId} in room {RoomId}", GetUserId(), roomId);
            }
        }

        #endregion

        #region Helper Methods

        private string GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        }

        private string GetUserName()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
        }

        private async Task<object> GetRoomInfo(int roomId)
        {
            var room = await _chatContext.Rooms
                .Include(r => r.Participants.Where(p => p.IsActive))
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null) return null!;

            return new
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                RoomType = room.RoomType,
                Status = room.Status,
                CurrentParticipants = room.CurrentParticipants,
                MaxCapacity = room.MaxCapacity,
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
                    GridPosition = p.GridPosition
                }).ToList()
            };
        }

        private static int? GetNextAvailableGridPosition(List<RoomParticipant> participants)
        {
            var usedPositions = participants
                .Where(p => p.GridPosition.HasValue)
                .Select(p => p.GridPosition!.Value)
                .ToHashSet();

            // Find first available position (1-20 for TikTok-style grid)
            for (int i = 1; i <= 20; i++)
            {
                if (!usedPositions.Contains(i))
                    return i;
            }

            return null; // Room is full
        }

        private async Task AddSystemMessage(int roomId, string content, string messageType)
        {
            try
            {
                var systemMessage = new Message
                {
                    RoomId = roomId,
                    SenderId = 0, // System message
                    Content = content,
                    MessageType = messageType,
                    CreatedAt = DateTime.UtcNow
                };

                _chatContext.Messages.Add(systemMessage);
                await _chatContext.SaveChangesAsync();

                await Clients.Group($"room_{roomId}").SendAsync("SystemMessage", new
                {
                    Id = systemMessage.Id,
                    Content = content,
                    MessageType = messageType,
                    CreatedAt = systemMessage.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding system message to room {RoomId}", roomId);
            }
        }

        private async Task UpdateParticipantStatusOnDisconnect(string userId)
        {
            try
            {
                var activeParticipants = await _chatContext.RoomParticipants
                    .Where(p => p.UserId == userId && p.IsActive)
                    .ToListAsync();

                foreach (var participant in activeParticipants)
                {
                    participant.Status = ParticipantStatus.Offline;
                    participant.LeftAt = DateTime.UtcNow;
                    participant.IsActive = false;

                    // Update room participant count
                    var room = await _chatContext.Rooms.FindAsync(participant.RoomId);
                    if (room != null)
                    {
                        room.CurrentParticipants = Math.Max(0, room.CurrentParticipants - 1);
                    }

                    // Notify room
                    await Clients.Group($"room_{participant.RoomId}").SendAsync("UserLeftRoom", new
                    {
                        UserId = userId,
                        Message = "Kullanıcı bağlantısı kesildi"
                    });
                }

                await _chatContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating participant status on disconnect for user {UserId}", userId);
            }
        }

        #endregion
    }
}
