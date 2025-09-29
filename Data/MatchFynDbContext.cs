using Microsoft.EntityFrameworkCore;
using MatchFynWebAPI.Models;

namespace MatchFynWebAPI.Data
{
    public class MatchFynDbContext : DbContext
    {
        public MatchFynDbContext(DbContextOptions<MatchFynDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Interest> Interests { get; set; }
        public DbSet<UserInterest> UserInterests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.Bio).HasMaxLength(500);
                entity.Property(e => e.ProfileImageUrl).HasMaxLength(255);
            });

            // Match entity configuration
            modelBuilder.Entity<Match>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Message).HasMaxLength(500);

                // Configure relationships
                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentMatches)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedMatches)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Interest entity configuration
            modelBuilder.Entity<Interest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // UserInterest entity configuration (many-to-many)
            modelBuilder.Entity<UserInterest>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.InterestId });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserInterests)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Interest)
                    .WithMany(i => i.UserInterests)
                    .HasForeignKey(e => e.InterestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed data for Interests
            modelBuilder.Entity<Interest>().HasData(
                new Interest { Id = 1, Name = "Spor", Description = "Spor aktiviteleri", Category = "Aktivite" },
                new Interest { Id = 2, Name = "Müzik", Description = "Müzik dinleme ve çalma", Category = "Sanat" },
                new Interest { Id = 3, Name = "Seyahat", Description = "Gezme ve seyahat etme", Category = "Yaşam" },
                new Interest { Id = 4, Name = "Kitap", Description = "Kitap okuma", Category = "Eğitim" },
                new Interest { Id = 5, Name = "Sinema", Description = "Film izleme", Category = "Eğlence" },
                new Interest { Id = 6, Name = "Yemek", Description = "Yemek yapma ve deneme", Category = "Yaşam" },
                new Interest { Id = 7, Name = "Teknoloji", Description = "Teknoloji ve gadget'lar", Category = "Teknoloji" },
                new Interest { Id = 8, Name = "Sanat", Description = "Resim, heykel ve sanat", Category = "Sanat" }
            );
        }
    }
}
