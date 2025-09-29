using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Models.Chat
{
    /// <summary>
    /// Room participant model for tracking users in chat rooms
    /// Implements professional tracking and status management
    /// </summary>
    public class RoomParticipant
    {
        public int Id { get; set; }

        /// <summary>
        /// Room ID reference
        /// </summary>
        public int RoomId { get; set; }
        public virtual Room Room { get; set; } = null!;

        /// <summary>
        /// User ID from Identity system
        /// </summary>
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User's display name in the room
        /// </summary>
        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// User's profile image URL
        /// </summary>
        [StringLength(500)]
        public string? ProfileImageUrl { get; set; }

        /// <summary>
        /// Participant role: Owner, Moderator, Member
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = ParticipantRoles.Member;

        /// <summary>
        /// Participant status: Online, Away, Offline
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = ParticipantStatus.Online;

        /// <summary>
        /// Is microphone enabled
        /// </summary>
        public bool IsMicrophoneEnabled { get; set; } = false;

        /// <summary>
        /// Is user currently speaking
        /// </summary>
        public bool IsSpeaking { get; set; } = false;

        /// <summary>
        /// User's position in TikTok-style grid layout
        /// </summary>
        public int? GridPosition { get; set; }

        /// <summary>
        /// When user joined the room
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When user left the room (null if still in room)
        /// </summary>
        public DateTime? LeftAt { get; set; }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total time spent in room (in minutes)
        /// </summary>
        public int TotalTimeMinutes { get; set; } = 0;

        /// <summary>
        /// Is participant currently active in room
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Connection ID for SignalR
        /// </summary>
        [StringLength(100)]
        public string? ConnectionId { get; set; }

        // Navigation properties
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    /// <summary>
    /// Static class for participant roles
    /// </summary>
    public static class ParticipantRoles
    {
        public const string Owner = "Owner";
        public const string Moderator = "Moderator";
        public const string Member = "Member";

        public static List<string> GetAllRoles()
        {
            return new List<string> { Owner, Moderator, Member };
        }

        public static Dictionary<string, string> GetRoleDescriptions()
        {
            return new Dictionary<string, string>
            {
                { Owner, "Room creator with full control" },
                { Moderator, "Can moderate chat and manage participants" },
                { Member, "Regular participant" }
            };
        }
    }

    /// <summary>
    /// Static class for participant status
    /// </summary>
    public static class ParticipantStatus
    {
        public const string Online = "Online";
        public const string Away = "Away";
        public const string Offline = "Offline";
        public const string Speaking = "Speaking";

        public static List<string> GetAllStatuses()
        {
            return new List<string> { Online, Away, Offline, Speaking };
        }
    }
}
