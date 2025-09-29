using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        
        [StringLength(10)]
        public string? Gender { get; set; }
        
        [StringLength(500)]
        public string? Bio { get; set; }
        
        [StringLength(255)]
        public string? ProfileImageUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<Match> SentMatches { get; set; } = new List<Match>();
        public virtual ICollection<Match> ReceivedMatches { get; set; } = new List<Match>();
        public virtual ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();
    }
}
