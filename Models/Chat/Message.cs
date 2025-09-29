using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Models.Chat
{
    /// <summary>
    /// Message model for chat system
    /// Supports text messages, reactions, and TikTok-style interactions
    /// </summary>
    public class Message
    {
        public int Id { get; set; }

        /// <summary>
        /// Room ID reference
        /// </summary>
        public int RoomId { get; set; }
        public virtual Room Room { get; set; } = null!;

        /// <summary>
        /// Sender participant ID
        /// </summary>
        public int SenderId { get; set; }
        public virtual RoomParticipant Sender { get; set; } = null!;

        /// <summary>
        /// Message content
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Message type: Text, Emoji, Reaction, System, Gift
        /// </summary>
        [Required]
        [StringLength(20)]
        public string MessageType { get; set; } = MessageTypes.Text;

        /// <summary>
        /// Reply to message ID (for threaded conversations)
        /// </summary>
        public int? ReplyToMessageId { get; set; }
        public virtual Message? ReplyToMessage { get; set; }

        /// <summary>
        /// Message timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Is message edited
        /// </summary>
        public bool IsEdited { get; set; } = false;

        /// <summary>
        /// Edit timestamp
        /// </summary>
        public DateTime? EditedAt { get; set; }

        /// <summary>
        /// Is message deleted
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Delete timestamp
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Message reactions (TikTok-style hearts, likes, etc.)
        /// </summary>
        public virtual ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();

        /// <summary>
        /// Replies to this message
        /// </summary>
        public virtual ICollection<Message> Replies { get; set; } = new List<Message>();
    }

    /// <summary>
    /// Message reaction model for TikTok-style interactions
    /// </summary>
    public class MessageReaction
    {
        public int Id { get; set; }

        /// <summary>
        /// Message ID reference
        /// </summary>
        public int MessageId { get; set; }
        public virtual Message Message { get; set; } = null!;

        /// <summary>
        /// User ID who reacted
        /// </summary>
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Reaction type: Heart, Like, Laugh, Wow, Sad, Angry
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ReactionType { get; set; } = string.Empty;

        /// <summary>
        /// Reaction emoji
        /// </summary>
        [StringLength(10)]
        public string? Emoji { get; set; }

        /// <summary>
        /// Reaction timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Static class for message types
    /// </summary>
    public static class MessageTypes
    {
        public const string Text = "Text";
        public const string Emoji = "Emoji";
        public const string Reaction = "Reaction";
        public const string System = "System";
        public const string Gift = "Gift";
        public const string Join = "Join";
        public const string Leave = "Leave";

        public static List<string> GetAllTypes()
        {
            return new List<string> { Text, Emoji, Reaction, System, Gift, Join, Leave };
        }
    }

    /// <summary>
    /// Static class for TikTok-style reaction types
    /// </summary>
    public static class ReactionTypes
    {
        public const string Heart = "Heart";
        public const string Like = "Like";
        public const string Laugh = "Laugh";
        public const string Wow = "Wow";
        public const string Sad = "Sad";
        public const string Angry = "Angry";
        public const string Fire = "Fire";
        public const string Clap = "Clap";

        public static Dictionary<string, string> GetReactionEmojis()
        {
            return new Dictionary<string, string>
            {
                { Heart, "❤️" },
                { Like, "👍" },
                { Laugh, "😂" },
                { Wow, "😮" },
                { Sad, "😢" },
                { Angry, "😠" },
                { Fire, "🔥" },
                { Clap, "👏" }
            };
        }
    }
}
