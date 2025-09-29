using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Models.Chat
{
    /// <summary>
    /// Room model for MatchFyn chat system
    /// Supports different room types: Waiting, Matching, Private, Public
    /// Follows clean architecture and professional standards
    /// </summary>
    public class Room
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Room type: Waiting, Matching, Private, Public
        /// </summary>
        [Required]
        [StringLength(20)]
        public string RoomType { get; set; } = string.Empty;

        /// <summary>
        /// Room status: Active, Inactive, Full, Closed
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Maximum capacity for the room
        /// </summary>
        [Range(1, 50)]
        public int MaxCapacity { get; set; }

        /// <summary>
        /// Current participant count
        /// </summary>
        public int CurrentParticipants { get; set; } = 0;

        /// <summary>
        /// Room creator user ID (from Identity system)
        /// </summary>
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        /// <summary>
        /// Gender filter for matching rooms (Male, Female, Mixed)
        /// </summary>
        [StringLength(10)]
        public string? GenderFilter { get; set; }

        /// <summary>
        /// Age range filter for matching rooms
        /// </summary>
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }

        /// <summary>
        /// Room duration in minutes (for temporary rooms)
        /// </summary>
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// Room expiry time (for temporary rooms)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Is this a premium/paid room
        /// </summary>
        public bool IsPremium { get; set; } = false;

        /// <summary>
        /// Room price for premium rooms
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Room creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Room activity status
        /// </summary>
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<RoomParticipant> Participants { get; set; } = new List<RoomParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<VoiceSession> VoiceSessions { get; set; } = new List<VoiceSession>();
    }

    /// <summary>
    /// Static class for room types - follows DRY principle
    /// </summary>
    public static class RoomTypes
    {
        public const string Waiting = "Waiting";
        public const string Matching = "Matching";
        public const string Private = "Private";
        public const string Public = "Public";

        public static List<string> GetAllTypes()
        {
            return new List<string> { Waiting, Matching, Private, Public };
        }

        public static Dictionary<string, (int maxCapacity, int? durationMinutes)> GetDefaultSettings()
        {
            return new Dictionary<string, (int, int?)>
            {
                { Waiting, (10, 15) },      // 10 people, 15 minutes
                { Matching, (20, 30) },     // 20 people, 30 minutes
                { Private, (4, null) },     // 4 people, no time limit
                { Public, (20, null) }      // 20 people, no time limit
            };
        }
    }

    /// <summary>
    /// Static class for room status - follows DRY principle
    /// </summary>
    public static class RoomStatus
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
        public const string Full = "Full";
        public const string Closed = "Closed";
        public const string Expired = "Expired";

        public static List<string> GetAllStatuses()
        {
            return new List<string> { Active, Inactive, Full, Closed, Expired };
        }
    }
}
