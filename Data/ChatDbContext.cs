using Microsoft.EntityFrameworkCore;
using MatchFynWebAPI.Models.Chat;

namespace MatchFynWebAPI.Data
{
    /// <summary>
    /// Chat DbContext for MatchFyn real-time communication system
    /// Implements Clean Architecture with separation of concerns
    /// Follows Code First approach with comprehensive configuration
    /// </summary>
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
        {
        }

        // DbSets for Chat entities
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomParticipant> RoomParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<VoiceSession> VoiceSessions { get; set; }
        public DbSet<VoiceActivity> VoiceActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Room entity
            ConfigureRoom(modelBuilder);

            // Configure RoomParticipant entity
            ConfigureRoomParticipant(modelBuilder);

            // Configure Message entity
            ConfigureMessage(modelBuilder);

            // Configure MessageReaction entity
            ConfigureMessageReaction(modelBuilder);

            // Configure VoiceSession entity
            ConfigureVoiceSession(modelBuilder);

            // Configure VoiceActivity entity
            ConfigureVoiceActivity(modelBuilder);

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        /// <summary>
        /// Configure Room entity with proper constraints and indexes
        /// </summary>
        private static void ConfigureRoom(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Room>(entity =>
            {
                // Configure properties
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.RoomType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.Property(e => e.CreatedByUserId)
                    .HasMaxLength(450);

                entity.Property(e => e.GenderFilter)
                    .HasMaxLength(10);

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                // Create indexes for better performance
                entity.HasIndex(e => e.RoomType)
                    .HasDatabaseName("IX_Rooms_RoomType");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Rooms_Status");

                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_Rooms_IsActive");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Rooms_CreatedAt");

                entity.HasIndex(e => new { e.RoomType, e.Status, e.IsActive })
                    .HasDatabaseName("IX_Rooms_Type_Status_Active");
            });
        }

        /// <summary>
        /// Configure RoomParticipant entity
        /// </summary>
        private static void ConfigureRoomParticipant(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RoomParticipant>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ProfileImageUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Member");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Online");

                entity.Property(e => e.ConnectionId)
                    .HasMaxLength(100);

                entity.Property(e => e.JoinedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.LastActivityAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                // Configure relationships
                entity.HasOne(e => e.Room)
                    .WithMany(r => r.Participants)
                    .HasForeignKey(e => e.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create indexes
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_RoomParticipants_UserId");

                entity.HasIndex(e => e.RoomId)
                    .HasDatabaseName("IX_RoomParticipants_RoomId");

                entity.HasIndex(e => new { e.RoomId, e.UserId })
                    .IsUnique()
                    .HasDatabaseName("IX_RoomParticipants_Room_User");

                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_RoomParticipants_IsActive");
            });
        }

        /// <summary>
        /// Configure Message entity
        /// </summary>
        private static void ConfigureMessage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.MessageType)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Text");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsEdited)
                    .HasDefaultValue(false);

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);

                // Configure relationships
                entity.HasOne(e => e.Room)
                    .WithMany(r => r.Messages)
                    .HasForeignKey(e => e.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Sender)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ReplyToMessage)
                    .WithMany(m => m.Replies)
                    .HasForeignKey(e => e.ReplyToMessageId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Create indexes
                entity.HasIndex(e => e.RoomId)
                    .HasDatabaseName("IX_Messages_RoomId");

                entity.HasIndex(e => e.SenderId)
                    .HasDatabaseName("IX_Messages_SenderId");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Messages_CreatedAt");

                entity.HasIndex(e => new { e.RoomId, e.CreatedAt })
                    .HasDatabaseName("IX_Messages_Room_CreatedAt");
            });
        }

        /// <summary>
        /// Configure MessageReaction entity
        /// </summary>
        private static void ConfigureMessageReaction(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageReaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.ReactionType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Emoji)
                    .HasMaxLength(10);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configure relationships
                entity.HasOne(e => e.Message)
                    .WithMany(m => m.Reactions)
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create unique constraint (one reaction per user per message)
                entity.HasIndex(e => new { e.MessageId, e.UserId, e.ReactionType })
                    .IsUnique()
                    .HasDatabaseName("IX_MessageReactions_Message_User_Type");
            });
        }

        /// <summary>
        /// Configure VoiceSession entity
        /// </summary>
        private static void ConfigureVoiceSession(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VoiceSession>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.Property(e => e.AudioQuality)
                    .HasMaxLength(20)
                    .HasDefaultValue("Medium");

                entity.Property(e => e.ConnectionId)
                    .HasMaxLength(100);

                entity.Property(e => e.PeerConnectionId)
                    .HasMaxLength(100);

                entity.Property(e => e.Metadata)
                    .HasMaxLength(1000);

                entity.Property(e => e.StartedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.LastActivityAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configure relationships
                entity.HasOne(e => e.Room)
                    .WithMany(r => r.VoiceSessions)
                    .HasForeignKey(e => e.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create indexes
                entity.HasIndex(e => e.RoomId)
                    .HasDatabaseName("IX_VoiceSessions_RoomId");

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_VoiceSessions_UserId");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_VoiceSessions_Status");
            });
        }

        /// <summary>
        /// Configure VoiceActivity entity
        /// </summary>
        private static void ConfigureVoiceActivity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VoiceActivity>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ActivityType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.StartedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configure relationships
                entity.HasOne(e => e.VoiceSession)
                    .WithMany(vs => vs.VoiceActivities)
                    .HasForeignKey(e => e.VoiceSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create indexes
                entity.HasIndex(e => e.VoiceSessionId)
                    .HasDatabaseName("IX_VoiceActivities_VoiceSessionId");

                entity.HasIndex(e => e.ActivityType)
                    .HasDatabaseName("IX_VoiceActivities_ActivityType");
            });
        }

        /// <summary>
        /// Seed initial data for rooms and configurations
        /// </summary>
        private static void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed default public rooms
            modelBuilder.Entity<Room>().HasData(
                new Room
                {
                    Id = 1,
                    Name = "Genel Sohbet",
                    Description = "Herkese açık genel sohbet odası",
                    RoomType = RoomTypes.Public,
                    Status = RoomStatus.Active,
                    MaxCapacity = 20,
                    GenderFilter = "Mixed",
                    IsPremium = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Room
                {
                    Id = 2,
                    Name = "Müzik Severler",
                    Description = "Müzik hakkında sohbet odası",
                    RoomType = RoomTypes.Public,
                    Status = RoomStatus.Active,
                    MaxCapacity = 20,
                    GenderFilter = "Mixed",
                    IsPremium = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Room
                {
                    Id = 3,
                    Name = "Spor Konuşmaları",
                    Description = "Spor ve fitness hakkında sohbet",
                    RoomType = RoomTypes.Public,
                    Status = RoomStatus.Active,
                    MaxCapacity = 20,
                    GenderFilter = "Mixed",
                    IsPremium = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            );
        }
    }
}
