using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MatchFynWebAPI.Models.Identity;

namespace MatchFynWebAPI.Data
{
    /// <summary>
    /// Identity DbContext for authentication and authorization
    /// Implements Clean Architecture with separation of concerns
    /// Follows Code First approach with comprehensive configuration
    /// </summary>
    public class IdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser entity
            ConfigureApplicationUser(builder);

            // Configure ApplicationRole entity
            ConfigureApplicationRole(builder);

            // Seed initial data
            SeedInitialData(builder);

            // Customize Identity table names (optional)
            CustomizeTableNames(builder);
        }

        /// <summary>
        /// Configure ApplicationUser entity with proper constraints and indexes
        /// </summary>
        private static void ConfigureApplicationUser(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>(entity =>
            {
                // Configure properties
                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.DateOfBirth)
                    .IsRequired();

                entity.Property(e => e.Gender)
                    .HasMaxLength(10);

                entity.Property(e => e.City)
                    .HasMaxLength(100);

                entity.Property(e => e.ProfileImageUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.Bio)
                    .HasMaxLength(1000);

                entity.Property(e => e.RefreshToken)
                    .HasMaxLength(500);

                // Create indexes for better performance
                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_ApplicationUsers_Email");

                entity.HasIndex(e => e.PhoneNumber)
                    .HasDatabaseName("IX_ApplicationUsers_PhoneNumber");

                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_ApplicationUsers_IsActive");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_ApplicationUsers_CreatedAt");

                // Configure default values
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.IsEmailVerified)
                    .HasDefaultValue(false);

                entity.Property(e => e.IsPhoneVerified)
                    .HasDefaultValue(false);
            });
        }

        /// <summary>
        /// Configure ApplicationRole entity
        /// </summary>
        private static void ConfigureApplicationRole(ModelBuilder builder)
        {
            builder.Entity<ApplicationRole>(entity =>
            {
                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.Priority)
                    .HasDefaultValue(0);

                // Create index for active roles
                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_ApplicationRoles_IsActive");
            });
        }

        /// <summary>
        /// Seed initial roles and admin user
        /// </summary>
        private static void SeedInitialData(ModelBuilder builder)
        {
            // Seed roles
            var roles = ApplicationRoles.GetRolesWithDescriptions();
            var roleEntities = new List<ApplicationRole>();
            int priority = 100;

            foreach (var role in roles)
            {
                roleEntities.Add(new ApplicationRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = role.Key,
                    NormalizedName = role.Key.ToUpper(),
                    Description = role.Value,
                    Priority = priority,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                priority -= 10; // Admin gets highest priority
            }

            builder.Entity<ApplicationRole>().HasData(roleEntities);

            // Note: Admin user seeding will be handled in a separate seeding service
            // to avoid hardcoding passwords in migrations
        }

        /// <summary>
        /// Customize Identity table names with MatchFyn prefix
        /// </summary>
        private static void CustomizeTableNames(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>().ToTable("MatchFyn_Users");
            builder.Entity<ApplicationRole>().ToTable("MatchFyn_Roles");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("MatchFyn_UserRoles");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("MatchFyn_UserClaims");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("MatchFyn_UserLogins");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("MatchFyn_UserTokens");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("MatchFyn_RoleClaims");
        }
    }
}
