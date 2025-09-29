using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Models.Identity
{
    /// <summary>
    /// Extended IdentityRole for MatchFyn application
    /// Implements role-based authorization with additional metadata
    /// </summary>
    public class ApplicationRole : IdentityRole
    {
        /// <summary>
        /// Role description for better understanding
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Role creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Role status - for enabling/disabling roles
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Role priority level (higher number = higher priority)
        /// Used for hierarchical role management
        /// </summary>
        public int Priority { get; set; } = 0;

        // Constructor for easy role creation
        public ApplicationRole() : base()
        {
        }

        public ApplicationRole(string roleName) : base(roleName)
        {
            Name = roleName;
            NormalizedName = roleName.ToUpper();
        }

        public ApplicationRole(string roleName, string description) : this(roleName)
        {
            Description = description;
        }
    }

    /// <summary>
    /// Static class containing predefined role names
    /// Follows DRY principle and prevents magic strings
    /// </summary>
    public static class ApplicationRoles
    {
        public const string Admin = "Admin";
        public const string Moderator = "Moderator";
        public const string User = "User";
        public const string PremiumUser = "PremiumUser";

        /// <summary>
        /// Get all available roles
        /// </summary>
        public static List<string> GetAllRoles()
        {
            return new List<string> { Admin, Moderator, User, PremiumUser };
        }

        /// <summary>
        /// Get role with description
        /// </summary>
        public static Dictionary<string, string> GetRolesWithDescriptions()
        {
            return new Dictionary<string, string>
            {
                { Admin, "System administrator with full access" },
                { Moderator, "Content moderator with limited admin access" },
                { User, "Regular user with standard features" },
                { PremiumUser, "Premium user with extended features" }
            };
        }
    }
}
