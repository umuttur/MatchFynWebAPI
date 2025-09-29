using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Models.Identity
{
    /// <summary>
    /// Extended IdentityUser for MatchFyn application
    /// Follows Clean Architecture principles with clear separation of concerns
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// User's full name - required field
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// User's date of birth - required for age verification
        /// </summary>
        [Required]
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// User's gender - optional field
        /// </summary>
        [StringLength(10)]
        public string? Gender { get; set; }

        /// <summary>
        /// User's city/province - optional field
        /// </summary>
        [StringLength(100)]
        public string? City { get; set; }

        /// <summary>
        /// Profile image URL - optional field
        /// </summary>
        [StringLength(500)]
        public string? ProfileImageUrl { get; set; }

        /// <summary>
        /// User biography - optional field
        /// </summary>
        [StringLength(1000)]
        public string? Bio { get; set; }

        /// <summary>
        /// Account creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Account status - for soft delete functionality
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Email verification status
        /// </summary>
        public bool IsEmailVerified { get; set; } = false;

        /// <summary>
        /// Phone verification status
        /// </summary>
        public bool IsPhoneVerified { get; set; } = false;

        /// <summary>
        /// Last login timestamp
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Refresh token for JWT authentication
        /// </summary>
        [StringLength(500)]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Refresh token expiry date
        /// </summary>
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Navigation properties will be added when integrating with main User model
        // Following Single Responsibility Principle - this class handles only Identity concerns
    }
}
