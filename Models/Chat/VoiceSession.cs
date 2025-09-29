using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Models.Chat
{
    /// <summary>
    /// Voice session model for tracking voice chat activities
    /// Supports TikTok Live-style voice interactions
    /// </summary>
    public class VoiceSession
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
        /// Session start time
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Session end time
        /// </summary>
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// Total duration in seconds
        /// </summary>
        public int DurationSeconds { get; set; } = 0;

        /// <summary>
        /// Voice session status: Active, Muted, Ended
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = VoiceSessionStatus.Active;

        /// <summary>
        /// Audio quality: Low, Medium, High
        /// </summary>
        [StringLength(20)]
        public string AudioQuality { get; set; } = "Medium";

        /// <summary>
        /// Is user currently speaking
        /// </summary>
        public bool IsSpeaking { get; set; } = false;

        /// <summary>
        /// Speaking time in seconds
        /// </summary>
        public int SpeakingTimeSeconds { get; set; } = 0;

        /// <summary>
        /// Voice activity level (0-100)
        /// </summary>
        [Range(0, 100)]
        public int ActivityLevel { get; set; } = 0;

        /// <summary>
        /// Connection quality score (0-100)
        /// </summary>
        [Range(0, 100)]
        public int ConnectionQuality { get; set; } = 100;

        /// <summary>
        /// WebRTC connection ID
        /// </summary>
        [StringLength(100)]
        public string? ConnectionId { get; set; }

        /// <summary>
        /// Peer connection ID for WebRTC
        /// </summary>
        [StringLength(100)]
        public string? PeerConnectionId { get; set; }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Session metadata (JSON format for additional data)
        /// </summary>
        [StringLength(1000)]
        public string? Metadata { get; set; }

        // Navigation properties
        public virtual ICollection<VoiceActivity> VoiceActivities { get; set; } = new List<VoiceActivity>();
    }

    /// <summary>
    /// Voice activity tracking for detailed analytics
    /// </summary>
    public class VoiceActivity
    {
        public int Id { get; set; }

        /// <summary>
        /// Voice session ID reference
        /// </summary>
        public int VoiceSessionId { get; set; }
        public virtual VoiceSession VoiceSession { get; set; } = null!;

        /// <summary>
        /// Activity type: Speaking, Listening, Muted, Disconnected
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ActivityType { get; set; } = string.Empty;

        /// <summary>
        /// Activity start time
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Activity end time
        /// </summary>
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// Duration in seconds
        /// </summary>
        public int DurationSeconds { get; set; } = 0;

        /// <summary>
        /// Audio level during activity (0-100)
        /// </summary>
        [Range(0, 100)]
        public int AudioLevel { get; set; } = 0;
    }

    /// <summary>
    /// Static class for voice session status
    /// </summary>
    public static class VoiceSessionStatus
    {
        public const string Active = "Active";
        public const string Muted = "Muted";
        public const string Ended = "Ended";
        public const string Disconnected = "Disconnected";

        public static List<string> GetAllStatuses()
        {
            return new List<string> { Active, Muted, Ended, Disconnected };
        }
    }

    /// <summary>
    /// Static class for voice activity types
    /// </summary>
    public static class VoiceActivityTypes
    {
        public const string Speaking = "Speaking";
        public const string Listening = "Listening";
        public const string Muted = "Muted";
        public const string Disconnected = "Disconnected";

        public static List<string> GetAllTypes()
        {
            return new List<string> { Speaking, Listening, Muted, Disconnected };
        }
    }
}
