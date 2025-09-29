using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<string> Interests { get; set; } = new List<string>();
    }

    public class CreateUserDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Phone]
        public string? PhoneNumber { get; set; }
        
        [Required]
        public DateTime DateOfBirth { get; set; }
        
        public string? Gender { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public List<int> InterestIds { get; set; } = new List<int>();
    }

    public class UpdateUserDto
    {
        [StringLength(100)]
        public string? Name { get; set; }
        
        [Phone]
        public string? PhoneNumber { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public List<int>? InterestIds { get; set; }
    }
}
